using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text;
using PhysicsFSCS;

namespace PhysicsFSCS.HTTP
{
	public class Server
	{
		private readonly RequestDelegate _next;

		public Server(RequestDelegate next)
		{
			_next = next;
		}

		public Task InvokeAsync(HttpContext context)
		{
			try {
				return Serve(context);
			} catch (DirectoryNotFoundException) {
				if (context.Response.HasStarted)
					throw;
				if (context.Request.Method == "PUT")
					return Put(context, fileInfo: null);
				else
					return StatusPage(context, 404, "Not found");
			} catch (FileNotFoundException) {
				if (context.Response.HasStarted)
					throw;
				if (context.Request.Method == "PUT")
					return Put(context, fileInfo: null);
				else
					return StatusPage(context, 404, "Not found");
			}
		}

		Task Serve(HttpContext context)
		{
			var fileInfo = FileSystem.Stat(context.Request.Path);

			if (fileInfo == null) {
				if (context.Request.Method == "PUT")
					return Put(context, fileInfo: null);
				else
					return StatusPage(context, 404, "Not found");
			} else {
				switch(context.Request.Method) {
					case "GET":
						return Get(context, fileInfo, isHead: false);
					case "PUT":
						return Put(context, fileInfo);
					case "DELETE":
						return Delete(context, fileInfo);
					case "HEAD":
						return Get(context, fileInfo, isHead: false);
					case "POST":
					default:
						return StatusPage(context, 405, "Method not allowed");
				}
			}
		}

		async Task Get(HttpContext context, PhysicsFSFileInfo fileInfo, bool isHead)
		{
			if (context.Request.Query.ContainsKey("info")) {
				var body = new StringBuilder();
				body.AppendLine("<!doctype HTML>");
				body.AppendLine("<html>");
				body.AppendLine("<head>");
				body.Append("<title>");
				body.Append(fileInfo.Name);
				body.AppendLine("</title>");
				body.AppendLine("</head>");
				body.AppendLine("<body>");
				body.Append("<h1>");
				body.Append(fileInfo.Name);
				body.AppendLine("</h1>");
				body.AppendLine("<dl>");

				body.AppendLine("<dt>Name:</dt>");
				body.Append("<dd>");
				body.Append(fileInfo.Name);
				body.AppendLine("</dd>");

				body.AppendLine("<dt>Length:</dt>");
				body.Append("<dd>");
				body.Append(fileInfo.Length);
				body.AppendLine("</dd>");

				body.AppendLine("<dt>IsReadOnly:</dt>");
				body.Append("<dd>");
				body.Append(fileInfo.IsReadOnly);
				body.AppendLine("</dd>");

				body.AppendLine("<dt>Type:</dt>");
				body.Append("<dd>");
				body.Append(fileInfo.Type);
				body.AppendLine("</dd>");

				body.AppendLine("<dt>ModTime:</dt>");
				body.Append("<dd>");
				body.Append(fileInfo.ModTime);
				body.AppendLine("</dd>");

				body.AppendLine("<dt>CreateTime:</dt>");
				body.Append("<dd>");
				body.Append(fileInfo.CreateTime);
				body.AppendLine("</dd>");

				body.AppendLine("<dt>AccessTime:</dt>");
				body.Append("<dd>");
				body.Append(fileInfo.AccessTime);
				body.AppendLine("</dd>");

				body.AppendLine("</dl>");
				body.AppendLine("</body>");
				body.AppendLine("</html>");

				var b = Encoding.UTF8.GetBytes(body.ToString());

				context.Response.StatusCode = 200;
				context.Response.ContentType = "text/html; charset=utf-8";
				context.Response.ContentLength = b.Length;

				if (!isHead)
					await context.Response.Body.WriteAsync(b, 0, b.Length, context.RequestAborted);
				return;
			}

			switch(fileInfo.Type) {
				case FileType.Regular:
					context.Response.StatusCode = 200;
					context.Response.ContentLength = fileInfo.Length;
					if (!isHead) {
						using(var stream = FileSystem.OpenRead(fileInfo.Name))
							stream.CopyTo(context.Response.Body);
					}
					return;
				case FileType.Directory:
					var body = new StringBuilder();
					body.AppendLine("<!doctype HTML>");
					body.AppendLine("<html>");
					body.AppendLine("<head>");
					body.Append("<title>");
					body.Append(fileInfo.Name);
					body.AppendLine("</title>");
					body.AppendLine("</head>");
					body.AppendLine("<body>");
					body.Append("<h1>");
					body.Append(fileInfo.Name);
					body.AppendLine("</h1>");
					body.AppendLine("<ul>");

					var trimmedName = fileInfo.Name.TrimEnd('/');
					if (trimmedName != "") {
						var idx = trimmedName.LastIndexOf('/');
						var parent = trimmedName.Substring(0, idx);
						body.Append("<li><a href='");
						body.Append(parent);
						body.Append("'>..</a></li>");
					}
					foreach(var name in FileSystem.EnumerateFiles(fileInfo.Name)) {
						var path = trimmedName + '/' + name;
						body.Append("<li><a href='");
						body.Append(path);
						body.Append("'>");
						body.Append(name);
						if (FileSystem.Stat(path).Type == FileType.Directory)
							body.Append('/');
						body.AppendLine("</a></li>");
					}
					body.AppendLine("</ul>");
					body.AppendLine("</body>");
					body.AppendLine("</html>");

					var b = Encoding.UTF8.GetBytes(body.ToString());

					context.Response.StatusCode = 200;
					context.Response.ContentType = "text/html; charset=utf-8";
					context.Response.ContentLength = b.Length;

					if (!isHead)
						await context.Response.Body.WriteAsync(b, 0, b.Length, context.RequestAborted);
				return;
				default:
					await StatusPage(context, 500, "Internal server error");
					return;
			}
		}

		async Task Put(HttpContext context, PhysicsFSFileInfo fileInfo)
		{
			if (fileInfo != null)
				FileSystem.Delete(fileInfo.Name);

			using(var w = FileSystem.OpenWrite(context.Request.Path))
			{
				await context.Request.Body.CopyToAsync(w, 1024, context.RequestAborted);
			}
		}

		Task Delete(HttpContext context, PhysicsFSFileInfo fileInfo)
		{
			FileSystem.Delete(fileInfo.Name);
			context.Response.StatusCode = 200;
			return Task.CompletedTask;
		}

		async Task StatusPage(HttpContext context, int code, string reason)
		{
			var body = new StringBuilder();
			body.AppendLine("<!doctype HTML>");
			body.AppendLine("<html>");
			body.AppendLine("<head>");
			body.Append("<title>");
			body.Append(code.ToString());
			body.Append(" ");
			body.Append(reason);
			body.AppendLine("</title>");
			body.AppendLine("</head>");
			body.AppendLine("<body>");
			body.Append("<h1>");
			body.Append(code.ToString());
			body.Append(" ");
			body.Append(reason);
			body.AppendLine("</h1>");
			body.AppendLine("</body>");
			body.AppendLine("</html>");

			var b = Encoding.UTF8.GetBytes(body.ToString());

			context.Response.StatusCode = code;
			context.Response.ContentType = "text/html; charset=utf-8";
			context.Response.ContentLength = b.Length;

			await context.Response.Body.WriteAsync(b, 0, b.Length, context.RequestAborted);
		}
	}
}
