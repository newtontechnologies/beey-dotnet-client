#How to configure Beey instance

## You need to:
1) navigate to ProxyIntegrations in Settings(.Overrides).xml
2) Ensure that 'Enabled="true"' is set
3) Add new 'Integrations' setting for this demo app
4) Set <URL>http://localhost:61384</Url> (if using default port on localhost)
 - when not working on localhost set it to address on which the Beey instance sees this Demo app
5) Set <BeeyUrl>http://localhost:61497</BeeyUrl> (if using default beey instance on localhost)
 - when not working on localhost set it to address on which the Demo app see the beey intance
6) Set the 'ProxyEndpoint' variable to "/demo" (if you want to change the endpoint use lowercase,c ase sensitivyty can change with different hosting endvirnment)
7) You should end up with setting like:
```xml
    <Integrations Name="3" ProxyEndpoint="/demo">
      <Url>http://localhost:61384</Url>
      <BeeyUrl>http://localhost:61497</BeeyUrl>
      <IconEndpoint>/favicon.ico</IconEndpoint>
      <ProjectEndpoint></ProjectEndpoint>

      <ServiceNames Name="0" Text="Demo App"/>
      <BypassAuthRegex Name="0">/favicon.ico</BypassAuthRegex>

    </Integrations>
```
8) restart Beey instance
9) start this demo beey app
10) try if app is running by GET request without proxy (by default http://localhost:61384/)
 - it should return error `"Error, probably not requested through Beey instance"`
9) Login to beey and get authorization token
- You can use postman or webbrowser developer tools
- from content of /API/Login request get value of 'Token' property
10)Construct url to BeeyApp
- use path on which you can access beey GUI (default http://localhost:61497/)
- add '/apps/demo/' to path (if you changed then endpoint you have to change it here too)
- add Authorization token ?Authorization=TOKEN_FROM_9
- request the adress through browser (for example: http://localhost:61497/apps/demo/?Authorization=aee16ba7-02f9-4070-b184-909ece52869b)
11) Server should return Beey.Proxy.BeeyProxy class serialized as json
```json
{"server":"http://localhost:61497","integrationUrl":"http://localhost:61497/apps/demo","authToken":"aee16ba7-02f9-4070-b184-909ece52869b","userId":1}
```