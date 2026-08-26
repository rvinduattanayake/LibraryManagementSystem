using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<LibraryDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<IBookRegistrationService, BookRegistrationService>();
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IReturnService, ReturnService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IInquiryService, InquiryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFineService, FineService>();
builder.Services.AddScoped<IDueDateReminderService, DueDateReminderService>();
builder.Services.AddHostedService<DueDateReminderBackgroundService>();
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    const string librarianRole = "Librarian";
    if (!await roleManager.RoleExistsAsync(librarianRole))
    {
        await roleManager.CreateAsync(new IdentityRole(librarianRole));
    }

    const string memberRole = "Member";
    if (!await roleManager.RoleExistsAsync(memberRole))
    {
        await roleManager.CreateAsync(new IdentityRole(memberRole));
    }

    const string librarianEmail = "librarian@library.local";
    if (await userManager.FindByEmailAsync(librarianEmail) is null)
    {
        var librarian = new ApplicationUser
        {
            UserName = librarianEmail,
            Email = librarianEmail,
            EmailConfirmed = true,
        };
        var createResult = await userManager.CreateAsync(librarian, "Librarian#2026");
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(librarian, librarianRole);
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
