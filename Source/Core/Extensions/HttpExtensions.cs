﻿#region Copyright 2014 Exceptionless

// This program is free software: you can redistribute it and/or modify it 
// under the terms of the GNU Affero General Public License as published 
// by the Free Software Foundation, either version 3 of the License, or 
// (at your option) any later version.
// 
//     http://www.gnu.org/licenses/agpl-3.0.html

#endregion

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Web.Http;

namespace Exceptionless.Core.Extensions {
    public static class HttpExtensions {
        public static bool TryGetLoginInformation(this HttpRequestMessage request, out string userName, out string password) {
            userName = null;
            password = null;

            if (request == null)
                return false;

            AuthenticationHeaderValue auth = request.Headers.Authorization;
            if (auth == null || String.IsNullOrWhiteSpace(auth.Parameter) || String.IsNullOrWhiteSpace(auth.Scheme) || auth.Scheme != ExceptionlessHeaders.Basic)
                return false;

            string[] header = ParseAuthorizationHeader(auth.Parameter);
            if (header == null || header.Length != 2)
                return false;

            userName = header[0];
            password = header[1];

            return true;
        }

        public static bool SetUserPrincipal(this HttpRequestMessage request, IPrincipal principal) {
            if (request == null || principal == null)
                return false;

            request.GetRequestContext().Principal = principal;

            return true;
        }

        public static string GetAllMessages(this HttpError error, bool includeStackTrace = false) {
            var builder = new StringBuilder();
            HttpError current = error;
            while (current != null) {
                string message = includeStackTrace ? current.FormatMessageWithStackTrace() : current.Message;
                builder.Append(message);

                if (current.ContainsKey("InnerException")) {
                    builder.Append(" --> ");
                    current = current["InnerException"] as HttpError;
                } else
                    current = null;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Formats an error with the stack trace included.
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public static string FormatMessageWithStackTrace(this HttpError error) {
            if (!error.ContainsKey("ExceptionMessage") || !error.ContainsKey("ExceptionType") || !error.ContainsKey("StackTrace"))
                return error.Message;

            return String.Format("[{0}] {1}\r\nStack Trace:\r\n{2}{3}", error["ExceptionType"], error["ExceptionMessage"], error["StackTrace"], Environment.NewLine);
        }

        private static string[] ParseAuthorizationHeader(string authHeader) {
            string[] credentials = Encoding.ASCII.GetString(Convert.FromBase64String(authHeader)).Split(new[] { ':' });

            if (credentials.Length != 2 || String.IsNullOrEmpty(credentials[0]) || String.IsNullOrEmpty(credentials[1]))
                return null;

            return credentials;
        }
    }
}