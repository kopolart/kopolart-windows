using System;
using System.Collections.Generic;

namespace Kopolart.Windows.Domain.Helpers
{
    public static class UrlNormalizer
    {
        public static string NormalizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            try
            {
                int queryIndex = url.IndexOf('?');
                string baseUrl = queryIndex >= 0 ? url.Substring(0, queryIndex) : url;

                if (baseUrl.EndsWith("/"))
                {
                    baseUrl = baseUrl.TrimEnd('/');
                }

                if (queryIndex < 0)
                {
                    return baseUrl;
                }

                string queryString = url.Substring(queryIndex + 1);
                int hashIndex = queryString.IndexOf('#');
                string hash = "";
                if (hashIndex >= 0)
                {
                    hash = queryString.Substring(hashIndex);
                    queryString = queryString.Substring(0, hashIndex);
                }

                var parts = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);
                var preservedParams = new List<string>();

                foreach (var part in parts)
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length > 0)
                    {
                        var key = keyValue[0].ToLowerInvariant().Trim();
                        if (key.StartsWith("utm_") || key == "fbclid" || key == "gclid" || key == "ref")
                        {
                            continue;
                        }
                        preservedParams.Add(part);
                    }
                }

                if (preservedParams.Count > 0)
                {
                    return baseUrl + "?" + string.Join("&", preservedParams) + hash;
                }

                return baseUrl + hash;
            }
            catch
            {
                return url;
            }
        }
    }
}
