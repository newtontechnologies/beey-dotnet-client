#How to use Beey proxy middleware with ASP.net core webserver

1) Add `app.UseBeeyProxy();` To Startup.cs `Configuration` method
- It have to be called early before the middlewares that will inject `BeeyProxy` class
2) Add `services.AddBeeyProxy();` To Startup.cs `ConfigureServices` method
- It also need to be called before any other services that use `BeeyProxy` class