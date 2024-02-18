using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using EOIotServer;
using EOIotServer.protocol;
using Microsoft.AspNetCore.Mvc;
using WAIotServer.Common;
using WAIotServer.Logic;

namespace WAIotServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            _ = new cls_core(typeof(Program));

            // Add services to the container.

            cls_log.get_default_().T_("", "");
            cls_log.get_default_().T_("", "");
            cls_log.get_default_().T_("", "================ ================");
            cls_log.get_default_().T_("", "Web启动");

            CGlobal global = new CGlobal();

            // 创建会话管理
            _ = new eow_session();

            // 启动一个数据服务
            _ = new CServer_HJ212();

            builder.Services.AddCors(
                options => options.AddPolicy(
                    "CORS",
                    configurePolicy => configurePolicy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                )
            );

            builder.Services.AddControllers(configure => configure.Filters.Add(new CWebFilter()));
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();
            app.UseCors("CORS");

            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}