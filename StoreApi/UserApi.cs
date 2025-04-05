

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mail;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net;
using TicketApi.Model;
using System.Text.Json;


namespace StoreApi
{
    
    public static class UserApi
    {
        public static void MapRoutes(WebApplication app) {

            app.MapPost("/login", async (LoginDto loginData) =>
            {
                if (string.IsNullOrWhiteSpace(loginData.Login) || string.IsNullOrWhiteSpace(loginData.Password))
                {
                    return Results.BadRequest(new { error = "Логин и пароль обязательны." });
                }
                PostgresContext db = new PostgresContext();
                var employee = await db.Employees
                    .Include(e => e.Store)
                    .FirstOrDefaultAsync(e => e.Login == loginData.Login && e.Password == loginData.Password);

                if (employee is null)
                {
                    return Results.Unauthorized();
                }

                // Генерация токенов
                var accessToken = GenerateAccessToken(employee);
                var refreshToken = Guid.NewGuid().ToString();

                // Обновление токенов у сотрудника
                employee.RefreshToken = refreshToken;
                employee.RefreshtokenExpiredtime = DateTime.UtcNow.AddHours(24).ToLocalTime();
                await db.SaveChangesAsync();

                // Формируем ответ
                var response = new
                {
                    token = accessToken,
                    refreshToken = refreshToken,
                    username = employee.EmployeeName,
                    login = employee.Login,
                    password = employee.Password,
                    contactInfo = employee.ContactInfo,
                    userId = Convert.ToString(employee.EmployeeId)
                };

                return Results.Json(response);
            });

            app.MapGet("/orders", () =>
            {
                PostgresContext db = new PostgresContext();
                var orders = db.Sales
                    .Include(s => s.Client)
                    .Select(s => new
                    {
                        OrderId = s.SaleId,
                        ClientId = s.Client.ClientId,
                        ClientName = s.Client.ClientName,
                        Address = s.DeliveryAddress,
                        OrderDate = s.SaleDate,
                        DiscountPercent = s.DiscountPercent,
                        TotalAmount = s.TotalAmount,
                        DeliveryDate = s.DeliveryDate
                    })
                    .ToList();
                Console.WriteLine(orders[0]);
                return Results.Json(orders);
            });

            app.MapPost("/ordersupdate", async (OrderUpdateDto updateData) =>
            {
                if (updateData == null)
                    return Results.BadRequest("Неверные данные");
                PostgresContext db = new PostgresContext();
                var order = await db.Sales.FirstOrDefaultAsync(s => s.SaleId == updateData.OrderId);
                if (order == null)
                    return Results.NotFound("Заказ не найден");

                var client = await db.Clients.FirstOrDefaultAsync(c => c.ClientName == updateData.ClientName);
                if (client == null)
                    return Results.NotFound("Клиент не найден");

                order.ClientId = client.ClientId;
                order.DeliveryAddress = updateData.Address;
                order.SaleDate = updateData.OrderDate;
                order.DiscountPercent = (decimal)updateData.DiscountPercent;
                order.TotalAmount = (decimal)updateData.TotalAmount;
                order.DeliveryDate = updateData.DeliveryDate ?? order.DeliveryDate;

                await db.SaveChangesAsync();
                return Results.Ok("Обновление успешно");
            });





            app.MapPost("/refresh-token", async (HttpContext context) =>
            {
                PostgresContext db = new PostgresContext();
                using var reader = new StreamReader(context.Request.Body);
                string refrtok = await reader.ReadToEndAsync();
                
                var pers = db.Employees.FirstOrDefault(u => u.RefreshToken == refrtok);

                if (pers == null || pers.RefreshtokenExpiredtime < DateTime.UtcNow.ToLocalTime())
                {
                    return Results.Content("LoginAgain");
                }
                var newAccessToken = GenerateAccessToken(pers);

                

                return Results.Content(newAccessToken); 
            });

           


        }



        public static string GenerateAccessToken (Employee pers)
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, Convert.ToString(pers.EmployeeId)),
                 
                };
            var claimsIdentity = new ClaimsIdentity(claims, "Token");
            // создаем JWT-токен
            var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(15)),
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
    public class LoginDto
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class OrderUpdateDto
{
    public int OrderId { get; set; }
    public string ClientName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public double DiscountPercent { get; set; }
    public double TotalAmount { get; set; }
    public DateTime? DeliveryDate { get; set; }
}


}
