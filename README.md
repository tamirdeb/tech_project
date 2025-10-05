# EKS Cluster with NGINX Deployment

This project contains Terraform files to create an EKS cluster on AWS and Kubernetes manifests to deploy an NGINX web server.

## Prerequisites

*   [Terraform](https://learn.hashicorp.com/tutorials/terraform/install-cli) installed
*   [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-install.html) installed and configured with your AWS credentials
*   [kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/) installed

## Deployment Steps

### 1. Provision the EKS Cluster and Addons

This single step will create the EKS cluster, its node groups, and install the AWS Load Balancer Controller addon.

1.  Initialize Terraform:
    ```bash
    terraform init
    ```

2.  (Optional) Plan the infrastructure changes:
    ```bash
    terraform plan
    ```

3.  Apply the Terraform configuration:
    ```bash
    terraform apply -auto-approve
    ```

### 2. Configure kubectl

After the EKS cluster is created, configure `kubectl` to communicate with it.

```bash
aws eks --region $(terraform output -raw region) update-kubeconfig --name $(terraform output -raw cluster_name)
```

### 3. Deploy NGINX

Deploy the NGINX application, service, and ingress.

```bash
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f ingress.yaml
```

### 4. Access NGINX

It might take a few minutes for the AWS Load Balancer to be provisioned. You can get the address of the load balancer by running:

```bash
kubectl get ingress nginx-ingress
```

Once the `ADDRESS` field is populated, you can access the NGINX welcome page by navigating to that address in your web browser.

### 5. Cleanup

To destroy all the resources created by this project, run:

```bash
terraform destroy -auto-approve
```