using ExpenseGuard.Api.Data;
using ExpenseGuard.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);




// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentEmployee, HeaderCurrentEmployee>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<IReceiptStorage, FakeReceiptStorage>();
    builder.Services.AddSingleton<IReceiptOcr, FakeReceiptOcr>();
}
else
{
    builder.Services.AddHttpClient<IReceiptStorage, CloudinaryReceiptStorage>();
    builder.Services.AddHttpClient<IReceiptOcr, OcrSpaceReceiptOcr>();
}
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseMiddleware<ApiExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
