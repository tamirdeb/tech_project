output "cluster_name" {
  description = "The name of the EKS cluster."
  value       = module.eks.cluster_name
}

output "cluster_endpoint" {
  description = "The endpoint for the EKS cluster."
  value       = module.eks.cluster_endpoint
}

output "cluster_kubeconfig_certificate_authority_data" {
  description = "The CA data for the EKS cluster."
  value       = module.eks.cluster_certificate_authority_data
}

output "region" {
    description = "AWS region"
    value = var.aws_region
}