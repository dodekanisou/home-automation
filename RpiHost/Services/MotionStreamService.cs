using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RpiHost.Services
{

    /// <summary>
    /// Service to stream the video stream from https://motion-project.github.io/. 
    /// Streaming is done in a "multipart/x-mixed-replace; boundary=BoundaryString"
    /// </summary>
    public class MotionStreamService
    {
        private readonly HttpClient _httpClient;

        // A couple of interesting articles found while researching how to stream 
        // https://www.tpeczek.com/2017/02/server-sent-events-sse-support-for.html
        // https://auth0.com/blog/building-a-reverse-proxy-in-dot-net-core/
        // https://blog.stephencleary.com/2016/11/streaming-zip-on-aspnet-core.html
        // https://github.com/StephenClearyExamples/AsyncDynamicZip/tree/core-ziparchive/Example


        public MotionStreamService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Stream motion feed to the http response.
        /// </summary>
        /// <param name="context">The http context</param>
        /// <param name="streamUrl">The motion stream url</param>
        /// <param name="bufferSize">Based on sampling, my reads never passed 9816</param>
        /// <returns></returns>
        public async Task StreamVideo(HttpContext context, Uri streamUrl, int bufferSize = 10 * 1024)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (streamUrl == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            using (var targetRequestMessage = CreateTargetMessage(context, streamUrl))
            {
                using (var responseMessage = await _httpClient.SendAsync(targetRequestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted).ConfigureAwait(false))
                {
                    context.Response.StatusCode = (int)responseMessage.StatusCode;
                    CopyFromTargetResponseHeaders(context, responseMessage);

                    byte[] buffer = new byte[bufferSize];

                    using (var stream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        // If you are hosting on IIS or Express this will never end
                        // https://github.com/aspnet/AspNetCoreModule/issues/38
                        while (!context.RequestAborted.IsCancellationRequested)
                        {
                            var bytesread = stream.Read(buffer, 0, buffer.Length);
                            await context.Response.Body.WriteAsync(buffer, 0, bytesread).ConfigureAwait(false);
                            await context.Response.Body.FlushAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private HttpRequestMessage CreateTargetMessage(HttpContext context, Uri targetUri)
        {
            var requestMessage = new HttpRequestMessage();
            CopyFromOriginalRequestContentAndHeaders(context, requestMessage);

            requestMessage.RequestUri = targetUri;
            requestMessage.Headers.Host = targetUri.Host;
            requestMessage.Method = GetMethod(context.Request.Method);

            return requestMessage;
        }


        private static void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage)
        {
            var requestMethod = context.Request.Method;

            if (!HttpMethods.IsGet(requestMethod) &&
              !HttpMethods.IsHead(requestMethod) &&
              !HttpMethods.IsDelete(requestMethod) &&
              !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(context.Request.Body);
                requestMessage.Content = streamContent;
            }

            foreach (var header in context.Request.Headers)
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        private static void CopyFromTargetResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
        {
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            // If I don't remove the header, the chunked content is not read
            context.Response.Headers.Remove("transfer-encoding");
        }

        private static HttpMethod GetMethod(string method)
        {
            if (HttpMethods.IsDelete(method)) return HttpMethod.Delete;
            if (HttpMethods.IsGet(method)) return HttpMethod.Get;
            if (HttpMethods.IsHead(method)) return HttpMethod.Head;
            if (HttpMethods.IsOptions(method)) return HttpMethod.Options;
            if (HttpMethods.IsPost(method)) return HttpMethod.Post;
            if (HttpMethods.IsPut(method)) return HttpMethod.Put;
            if (HttpMethods.IsTrace(method)) return HttpMethod.Trace;
            return new HttpMethod(method);
        }

    }
}
