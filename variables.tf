variable "aws_region" {
  description = "The AWS region to create resources in."
  default     = "us-east-1"
}

variable "cluster_version" {
  description = "The Kubernetes version for the EKS cluster."
  default     = "1.28"
}