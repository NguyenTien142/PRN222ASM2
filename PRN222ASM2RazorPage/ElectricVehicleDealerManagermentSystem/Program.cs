using ElectricVehicleDealerManagermentSystem.SignalR;
using Microsoft.EntityFrameworkCore;
using Repositories.Context;
using Repositories.CustomRepositories;
using Repositories.GenericRepository;
using Repositories.Interfaces;
using Repositories.UnitOfWork;
using Services.Helpper.Mapper;
using Services.Implements;
using Services.Interfaces;

namespace ElectricVehicleDealerManagermentSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            builder.Services.AddDbContext<Prn222asm2Context>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add session services
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            //auto mapper
            builder.Services.AddAutoMapper(cfg => { }, typeof(MapperProfile));

            // SignalR
            builder.Services.AddSignalR();

            // Add UnitOfWork
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IRepositoryFactory, RepositoryFactory>();

            // Add Generic Repositories
            builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

            // Add Custom Repositories
            builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<IDealerRepository, DealerRepository>();
            builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IVehicleCategoryRepository, VehicleCategoryRepository>();

            // Add Services
            builder.Services.AddScoped<IAppointmentServices, AppointmentServices>();
            builder.Services.AddScoped<ICustomerServices, CustomerServices>();
            builder.Services.AddScoped<IDealerServices, DealerServices>();
            builder.Services.AddScoped<IVehicleServices, VehicleServices>();
            builder.Services.AddScoped<IOrderServices, OrderServices>();
            builder.Services.AddScoped<IUserServices, UserServices>();
            builder.Services.AddScoped<IVehicleCategoryServices, VehicleCategoryServices>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Use session middleware
            app.UseSession();

            app.UseAuthorization();

            app.MapRazorPages();
            app.MapHub<SignalRHub>("/signalRHub");

            app.Run();
        }
    }
}
