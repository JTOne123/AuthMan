﻿using System;
using System.Threading.Tasks;
using AuthMan.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthMan
{
	public class AuthenticationMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<AuthenticationMiddleware> _logger;

		public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task Invoke(HttpContext context, IOptions<AuthManOptions> optsThing, IServiceProvider provider)
		{
			var opts = optsThing.Value;
			try
			{
				if (!context.Session.IsAvailable)
					await context.Session.LoadAsync();
				var authMan = (IAuthMan) ActivatorUtilities.CreateInstance(provider, opts.AuthMan ?? typeof(AuthMan));
				await authMan.Setup(context);
				context.Items["authMan"] = authMan;
				await _next(context);
			}
			catch (Exception e)
			{
				while (e.InnerException != null)
					e = e.InnerException;
				switch (e)
				{
					case NotSignedIn _:
					case NotAuthorized _:
						_logger.LogDebug(e.ToString());
						if (opts.RendererType == null) throw;
						var renderer = ActivatorUtilities.CreateInstance<IRenderer>(provider);
						await renderer.Handle(context);
						return;
					default:
						throw;
				}
			}
		}
		
	}
}
