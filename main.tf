provider "aws" {
  region = var.aws_region
}

data "aws_availability_zones" "available" {}

locals {
  cluster_name = "my-eks-cluster"
}

module "vpc" {
  source          = "terraform-aws-modules/vpc/aws"
  version         = "3.14.2"
  name            = "my-vpc"
  cidr            = "10.0.0.0/16"
  azs             = data.aws_availability_zones.available.names
  private_subnets = ["10.0.1.0/24", "10.0.2.0/24", "10.0.3.0/24"]
  public_subnets  = ["10.0.101.0/24", "10.0.102.0/24", "10.0.103.0/24"]
  enable_nat_gateway   = true
  single_nat_gateway   = true
  enable_dns_hostnames = true

  tags = {
    "kubernetes.io/cluster/${local.cluster_name}" = "shared"
  }

  public_subnet_tags = {
    "kubernetes.io/cluster/${local.cluster_name}" = "shared"
    "kubernetes.io/role/elb"                      = "1"
  }

  private_subnet_tags = {
    "kubernetes.io/cluster/${local.cluster_name}" = "shared"
    "kubernetes.io/role/internal-elb"             = "1"
  }
}

module "eks" {
  source          = "terraform-aws-modules/eks/aws"
  version         = "18.20.0"
  cluster_name    = local.cluster_name
  cluster_version = var.cluster_version
  vpc_id          = module.vpc.vpc_id
  subnets         = module.vpc.private_subnets

  cluster_addons = {
    aws-load-balancer-controller = {}
  }

  eks_managed_node_groups = {
    one = {
      min_size     = 1
      max_size     = 2
      desired_size = 1
      instance_type = "t2.medium"
    }
  }

  tags = {
    Environment = "dev"
    Terraform   = "true"
  }
}