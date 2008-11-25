﻿/// <summary> OWASP .NET Enterprise Security API (.NET ESAPI)
/// 
/// This file is part of the Open Web Application Security Project (OWASP)
/// .NET Enterprise Security API (.NET ESAPI) project. For details, please see
/// http://www.owasp.org/index.php/.NET_ESAPI.
/// 
/// Copyright (c) 2008 - The OWASP Foundation
/// 
/// The .NET ESAPI is published by OWASP under the LGPL. You should read and accept the
/// LICENSE before you use, modify, and/or redistribute this software.
/// 
/// </summary>
/// <author>  Alex Smolen <a href="http://www.foundstone.com">Foundstone</a>
/// </author>
/// <created>  2008 </created>

using System;
using System.Security.Principal;
using System.Web;
using Owasp.Esapi.Interfaces;
using Owasp.Esapi.Errors;
using HttpInterfaces;
using System.Collections;
using System.Web.SessionState;

namespace Owasp.Esapi.Filters
{
    /// <summary>
    /// This class is used to intercept web requests and apply appropriate ESAPI security checks.    
    /// </summary>
    public class EsapiFilter : IHttpModule
    {

        private static readonly ILogger logger = Esapi.Logger();

        private static readonly string[] ignore = { "password" };

        #region IHttpModule Members

        private void Application_BeginRequest(Object source, EventArgs e)
        {            
            HttpContext context = HttpContext.Current;
            HttpRequest request = (HttpRequest) context.Request;
            HttpResponse response = (HttpResponse) context.Response;
            try
            {
                //// figure out who the current user is                
                try
                {
                (                    
                    (Authenticator) Esapi.Authenticator()).Context = WebContext.Cast(HttpContext.Current);                    
                    Esapi.Authenticator().Login();
                }
                catch (AuthenticationException ex)
                {
                    ((Authenticator)Esapi.Authenticator()).Logout();
                    // FIXME: use safeforward!
                    // FIXME: make configurable with config
                    // int position = request.Url.ToString().LastIndexOf('/') + 1;
                    // string page = request.Url.ToString().Substring(position, request.Url.ToString().Length - position);
                    // if (!page.ToLower().Equals("default.aspx"))
                    // {
                    //    response.Redirect("default.aspx");   
                    // }                    
                    // return;
                }

                //// log this request, obfuscating any parameter named password
                ////logger.LogHttpRequest(new ArrayList (ignore));

                //// check access to this URL
                //if (!Esapi.AccessController().IsAuthorizedForUrl(request.RawUrl.ToString()))
                //{
                //    context.Items["message"] = "Unauthorized";
                //    context.Server.Transfer("login.aspx");                        
                //}
                // verify if this request meets the baseline input requirements                
                if (!Esapi.Validator().IsValidHttpRequest(WebContext.Cast(request)))
                {
                    context.Items["message"] = "Validation error";
                }

                // check for CSRF attacks and set appropriate caching headers
                IHttpUtilities utils = Esapi.HttpUtilities();
                utils.VerifyCsrfToken();
                utils.SetNoCacheHeaders();
                utils.SafeSetContentType();

                // forward this request on to the web application                
            }
            catch (Exception ex)
            {
                logger.Info(LogEventTypes.SECURITY, "Security error in ESAPI Filter", ex);
                response.Output.WriteLine("<H1>Security Error</H1>");
            }
        }

        private void Application_ReleaseRequestState(object sender, EventArgs objArgs)
        {
            IHttpUtilities utils = Esapi.HttpUtilities();
            utils.AddCsrfToken();
        }
    
        /// <summary>
        /// HttpModule Dispose method.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// HttpModule Init method
        /// </summary>
        /// <param name="context">The HTTP application context.</param>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(Application_BeginRequest);
            context.ReleaseRequestState += new EventHandler(Application_ReleaseRequestState);
        }

        #endregion
    }
}
