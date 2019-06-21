using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Filters
{
	public class Startup
	{
		 public void ConfigureServices(IServiceCollection services)
		{
			services.AddDataLoaderRegistry();
            
			services.AddGraphQL(sp => Schema.Create(c =>
			{
				c.RegisterQueryType<TestQueryType>();
			}));
		}
        
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
            
			app.UseGraphQL();
			app.UsePlayground();
		}
	}
}
