﻿using System.Configuration;
using Lending.Execution.Auth;
using Microsoft.Owin.Extensions;
using Owin;
using Owin.StatelessAuth;

namespace Lending.Execution
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var pathsToIgnore = new[]
            {
                "/authentication/**",
                "/authentication",
            };

            app.Map("/api", site =>
            {
                site.RequiresStatelessAuth(new SecureTokenValidator(ConfigurationManager.AppSettings["jwt_secret"]), new StatelessAuthOptions
                {
                    IgnorePaths = pathsToIgnore,
                    PassThroughUnauthorizedRequests = true
                });
                site.UseNancy();
                site.UseStageMarker(PipelineStage.MapHandler);
            });
        }
    }
}
