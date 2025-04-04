if [ ! -z "${PLOOMESDEV_DATADOG_TRACER_ON}" ] && [ "${PLOOMESDEV_DATADOG_TRACER_ON}" = true ]
then
    echo -e "Instalando Datadog APM Tracer para .NET / .NET Core\n===========\n"
    curl -LO https://github.com/DataDog/dd-trace-dotnet/releases/download/v1.28.7/datadog-dotnet-apm_1.28.7_amd64.deb && dpkg -i datadog-dotnet-apm_1.28.7_amd64.deb && /opt/datadog/createLogPath.sh
    gcho -e "\nTracer instalado com sucesso.\n\n"
fi
