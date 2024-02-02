az login

#Get-AzSubscription
Write-Debug "Selecting subscription..."
$subscription = "************"
az account set --subscription "$subscription"

az configure --defaults location="northeurope"

$rg='resourcegroup'
$storageaccount = 'storage'
$azFunctionName = 'mike-fun-serilog-demo'
$functionName = 'AIDemo'
$appInsights = $azFunctionName
$elasticUrl = "https://test-**********.alfalaval.org:9200"
$elasticProxyUrl = "https://$($azFunctionName).azurewebsites.net/api/elasticproxy"
$body='{}'


#Create function (an AI resources will be created as well) 
az functionapp create --name $azFunctionName --resource-group $rg --storage-account $storageaccount --functions-version 3 --consumption-plan-location northeurope

#Get the instrumentation key
$AIKey = $(az resource show -g $rg -n $appInsights --resource-type "microsoft.insights/components" --query properties.InstrumentationKey).Replace('"','')

#Set the instrumentation key as an app setting
az functionapp config appsettings set --name $azFunctionName --resource-group $rg --settings "APPINSIGHTS_INSTRUMENTATIONKEY = $AIKey"
az functionapp config appsettings set --name $azFunctionName --resource-group $rg --settings "ELASTIC_PROXY_URL = $elasticProxyUrl"
az functionapp config appsettings set --name $azFunctionName --resource-group $rg --settings "ELASTIC_URL = $elasticUrl"

# Set some variables
$baseurl = $(az functionapp function show --function-name $functionName --name $azFunctionName --resource-group $rg --query "invokeUrlTemplate" --output tsv)
$key = $(az functionapp keys list -g $rg -n $azFunctionName --query functionKeys.default)
$key = $key.Replace('"','')
$url = "${baseurl}?code=$key"

Write-Output "Base endpoint: $baseurl"
Write-Output "Function Key: $key"
Write-Output "Intrumentation Key: $AIKey"
Write-Output "Complete endpoint: $url"


#Send an empty request to trigger the function
curl $url

