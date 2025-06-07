var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
//builder.Services.AddIdentity<AppUser, IdentityRole>(opt =>
//{
//    
//}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

//builder.Services.AddDbContext<AppDbContext>(opt =>
//{

//    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"));

//});



var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();



app.MapControllerRoute(
        "default",
        "{controller=home}/{action=index}/{id?}"
         );


app.Run();
