#!/bin/bash
set -e

echo "====================================="
echo "Expense Management System Deployment"
echo "====================================="
echo ""

# Check if user is logged in to Azure
if ! az account show &> /dev/null; then
    echo "❌ Error: Not logged in to Azure CLI"
    echo "Please run: az login"
    exit 1
fi

# Variables
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

echo "📋 Deployment Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  Admin: $ADMIN_LOGIN"
echo "  Admin Object ID: $ADMIN_OBJECT_ID"
echo ""

# Create resource group
echo "🔨 Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none

# Deploy infrastructure (without GenAI)
echo "🚀 Deploying infrastructure (App Service + Azure SQL)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file infrastructure/main.bicep \
    --parameters location=$LOCATION adminObjectId=$ADMIN_OBJECT_ID adminLogin=$ADMIN_LOGIN deployGenAI=false \
    --output json)

echo "✅ Infrastructure deployment completed"

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.sqlServerFqdn.value')
SQL_DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.sqlDatabaseName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityName.value')

echo ""
echo "📦 Deployed Resources:"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Database: $SQL_DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  MI Client ID: $MANAGED_IDENTITY_CLIENT_ID"
echo ""

# Configure App Service settings
echo "⚙️  Configuring App Service settings..."
az webapp config appsettings set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --settings \
        "ConnectionStrings__DefaultConnection=Server=tcp:$SQL_SERVER_FQDN,1433;Database=$SQL_DATABASE_NAME;Authentication=Active Directory Managed Identity;User Id=$MANAGED_IDENTITY_CLIENT_ID;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
        "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
        "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
    --output none

echo "✅ App Service settings configured"

# Wait for resources to be fully ready
echo ""
echo "⏳ Waiting 30 seconds for resources to be fully provisioned..."
sleep 30

# Add current IP to SQL firewall
echo ""
echo "🔒 Configuring SQL Server firewall..."
MY_IP=$(curl -s https://api.ipify.org)
SQL_SERVER_NAME=$(echo $SQL_SERVER_FQDN | cut -d'.' -f1)

# Allow Azure services access
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowAllAzureIPs" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none 2>/dev/null || true

# Add deployment IP
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowDeploymentIP" \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP \
    --output none 2>/dev/null || true

echo "✅ Firewall rules configured"

# Wait for firewall rules to propagate
echo "⏳ Waiting 15 seconds for firewall rules to propagate..."
sleep 15

# Install Python dependencies
echo ""
echo "📦 Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with actual server names
echo "📝 Updating Python scripts with deployment values..."
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

# Update script.sql with managed identity name
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# Import database schema
echo ""
echo "📊 Importing database schema..."
python3 run-sql.py

# Configure database roles for managed identity
echo ""
echo "🔐 Configuring database permissions for managed identity..."
python3 run-sql-dbrole.py

# Deploy stored procedures
echo ""
echo "📜 Deploying stored procedures..."
python3 run-sql-stored-procs.py

# Build and package the application
echo ""
echo "🏗️  Building application..."
cd ExpenseManagementApp
dotnet restore --verbosity quiet
dotnet build --configuration Release --verbosity quiet
dotnet publish --configuration Release --output ./publish --verbosity quiet

# Create deployment package
echo "📦 Creating deployment package..."
cd publish
zip -r ../app.zip . > /dev/null
cd ../..

# Deploy application
echo ""
echo "🚀 Deploying application to App Service..."
az webapp deploy \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --src-path ExpenseManagementApp/app.zip \
    --type zip \
    --output none

echo ""
echo "============================================"
echo "✅ Deployment completed successfully!"
echo "============================================"
echo ""
echo "🌐 Application URL: https://$APP_SERVICE_NAME.azurewebsites.net/Index"
echo ""
echo "📝 Note: Navigate to /Index (not just the root URL)"
echo "💬 Chat UI available at: /Chat"
echo ""
echo "⚠️  GenAI features not deployed. Run deploy-with-chat.sh to enable AI chat."
echo ""
