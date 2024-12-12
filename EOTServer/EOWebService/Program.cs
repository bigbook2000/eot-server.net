using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
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
            cls_log.get_default_().T_("", "Web����");

            CGlobal global = new();

            //string s = $"return \"a\"+1;";
            //var script = CSharpScript.Create(s);
            //var ret = script.RunAsync();

            // �����Ự����
            _ = new eow_session();

            // ���� CORS
            // app.UseCors("CORS");
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

            // ��Ӷ�wwwroot��̬�ļ�֧��
            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}