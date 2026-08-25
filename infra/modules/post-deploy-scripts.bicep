param location string
param resourceTags object
param deploymentSuffix string
param deploymentScriptIdentityName string
param foundryAccountName string
param foundryProjectName string
param foundryIqBootstrapJobName string
param provisioningJobName string

resource foundryAccount 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = {
  name: foundryAccountName
}

resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' existing = {
  parent: foundryAccount
  name: foundryProjectName
}

resource deploymentScriptIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: deploymentScriptIdentityName
  location: location
  tags: resourceTags
}

resource deploymentScriptContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, deploymentScriptIdentity.id, 'Contributor', deploymentSuffix)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
    principalId: deploymentScriptIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource deploymentScriptFoundryContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryAccount.id, deploymentScriptIdentity.id, 'CognitiveServicesContributor', deploymentSuffix)
  scope: foundryAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '25fbc0a9-bd7c-42a3-aa1a-3b75d497ee68')
    principalId: deploymentScriptIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
  dependsOn: [
    foundryProject
  ]
}

resource deploymentScriptFoundryUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryAccount.id, deploymentScriptIdentity.id, 'FoundryUser', deploymentSuffix)
  scope: foundryAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '53ca6127-db72-4b80-b1b0-d745d6d5456d')
    principalId: deploymentScriptIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
  dependsOn: [
    foundryProject
  ]
}

// Deployment Scripts require storage shared-key authentication. A one-shot
// container group keeps post-deploy orchestration on Entra ID via this UAMI.
resource runPostDeployContainer 'Microsoft.ContainerInstance/containerGroups@2025-09-01' = {
  name: 'run-foundry-iq-bootstrap-${deploymentSuffix}'
  location: location
  tags: resourceTags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${deploymentScriptIdentity.id}': {}
    }
  }
  properties: {
    osType: 'Linux'
    restartPolicy: 'Never'
    containers: [
      {
        name: 'post-deploy-runner'
        properties: {
          image: 'mcr.microsoft.com/azure-cli:2.62.0'
          command: [
            '/bin/bash'
            '-c'
            '''
              set -euo pipefail

              install_containerapp_extension() {
                for attempt in $(seq 1 3); do
                  if az extension add --name containerapp --upgrade; then
                    return 0
                  fi

                  echo "Container Apps CLI extension installation attempt ${attempt} failed." >&2
                  sleep 10
                done

                echo "Unable to install the Container Apps CLI extension." >&2
                return 1
              }

              run_job() {
                local job_name="$1"
                local container_name="$2"
                local max_attempts="$3"

                echo "Starting Container Apps job ${job_name}..."
                local execution
                execution=$(az containerapp job start \
                  --name "${job_name}" \
                  --resource-group "${RESOURCE_GROUP}" \
                  --query name -o tsv)
                echo "Job execution: ${execution}"

                for i in $(seq 1 "${max_attempts}"); do
                  local status
                  status=$(az containerapp job execution show \
                    --name "${job_name}" \
                    --resource-group "${RESOURCE_GROUP}" \
                    --job-execution-name "${execution}" \
                    --query properties.status -o tsv)

                  echo "Job ${job_name} status: ${status}"

                  if [ "${status}" = "Succeeded" ]; then
                    echo "Job ${job_name} completed successfully."
                    return 0
                  fi

                  if [ "${status}" = "Failed" ]; then
                    echo "Job ${job_name} failed. Fetching recent logs..."
                    az containerapp job logs show \
                      --name "${job_name}" \
                      --resource-group "${RESOURCE_GROUP}" \
                      --execution "${execution}" \
                      --container "${container_name}" \
                      --tail 50 2>/dev/null || true
                    return 1
                  fi

                  sleep 15
                done

                echo "Timed out waiting for job ${job_name}."
                return 1
              }

              install_containerapp_extension
              echo "Waiting for role assignments and Foundry deployments to settle..."
              sleep 180
              az login --identity --username "${AZURE_CLIENT_ID}" --allow-no-subscriptions --output none
              az account set --subscription "${AZURE_SUBSCRIPTION_ID}"
              run_job "${FOUNDRY_IQ_BOOTSTRAP_JOB_NAME}" "foundry-iq-bootstrap" 180

              echo "Waiting for MCP health and role assignment propagation..."
              sleep 120
              run_job "${PROVISIONING_JOB_NAME}" "agent-provisioning" 120
            '''
          ]
          environmentVariables: [
            {
              name: 'AZURE_CLIENT_ID'
              value: deploymentScriptIdentity.properties.clientId
            }
            {
              name: 'AZURE_STORAGE_AUTH_MODE'
              value: 'login'
            }
            {
              name: 'AZURE_SUBSCRIPTION_ID'
              value: subscription().subscriptionId
            }
            {
              name: 'RESOURCE_GROUP'
              value: resourceGroup().name
            }
            {
              name: 'FOUNDRY_IQ_BOOTSTRAP_JOB_NAME'
              value: foundryIqBootstrapJobName
            }
            {
              name: 'PROVISIONING_JOB_NAME'
              value: provisioningJobName
            }
          ]
          resources: {
            requests: {
              cpu: 1
              memoryInGB: json('1.5')
            }
          }
        }
      }
    ]
  }
  dependsOn: [
    deploymentScriptContributorRole
  ]
}
