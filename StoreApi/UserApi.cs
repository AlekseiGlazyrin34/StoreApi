

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


            app.MapGet("/warehouse", () =>
            {
                using var db = new PostgresContext();
                var warehouseItems = db.Storebalances
                    .Include(sb => sb.Store)
                    .Include(sb => sb.Product)
                    .Select(sb => new
                    {
                        Id = sb.StoreId * 10000 + sb.ProductId, // Уникальный ID на основе составного ключа
                        StoreName = sb.Store.StoreAddress,
                        ProductName = sb.Product.ProductName,
                        Quantity = sb.Quantity
                    })
                    .ToList();

                return Results.Json(warehouseItems);
            });

            app.MapPost("/warehouseupdate", async (WarehouseItem updateItem) =>
            {
                using var db = new PostgresContext();

                var store = await db.Stores.FirstOrDefaultAsync(s => s.StoreAddress == updateItem.StoreName);
                var product = await db.Products.FirstOrDefaultAsync(p => p.ProductName == updateItem.ProductName);

                if (store == null || product == null)
                    return Results.NotFound("Магазин или продукт не найден");

                var balance = await db.Storebalances.FirstOrDefaultAsync(sb => sb.StoreId == store.StoreId && sb.ProductId == product.ProductId);

                if (balance == null)
                    return Results.NotFound("Запись склада не найдена");

                balance.Quantity = updateItem.Quantity;
                await db.SaveChangesAsync();
                return Results.Ok("Обновление успешно");

            });

            // Получение списка клиентов
            app.MapGet("/clients", () =>
            {
                PostgresContext db = new PostgresContext();
                var clients = db.Clients
                    .Select(c => new
                    {
                        ClientId = c.ClientId,
                        Name = c.ClientName,
                        ContactInfo = c.ContactInfo
                    })
                    .ToList();

                return Results.Json(clients);
            });

            // Обновление клиента
            app.MapPost("/clientsupdate", async (ClientDto clientData) =>
            {
                if (clientData == null)
                    return Results.BadRequest("Неверные данные");

                PostgresContext db = new PostgresContext();
                var client = await db.Clients.FirstOrDefaultAsync(c => c.ClientId == clientData.ClientId);

                if (client == null)
                    return Results.NotFound("Клиент не найден");

                client.ClientName = clientData.Name;
                client.ContactInfo = clientData.ContactInfo;

                await db.SaveChangesAsync();
                return Results.Ok("Обновление успешно");
            });

            app.MapGet("/employees", () =>
            {
                using var db = new PostgresContext();
                var employees = db.Employees
                    .Include(e => e.Store)
                    .Select(e => new
                    {
                        EmployeeId = e.EmployeeId,
                        Name = e.EmployeeName,
                        StoreId = e.StoreId,
                        StoreName = e.Store.StoreAddress,
                        ContactInfo = e.ContactInfo
                    })
                    .ToList();

                return Results.Json(employees);
            });

            app.MapPost("/employeesupdate", async (EmployeeDto empData) =>
            {
                if (empData == null)
                    return Results.BadRequest("Неверные данные");

                using var db = new PostgresContext();
                var employee = await db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == empData.EmployeeId);

                if (employee == null)
                    return Results.NotFound("Сотрудник не найден");

                employee.EmployeeName = empData.Name;
                employee.ContactInfo = empData.ContactInfo;

                await db.SaveChangesAsync();
                return Results.Ok("Обновление успешно");
            });

            app.MapGet("/partners", () =>
            {
                using var db = new PostgresContext();
                var partners = db.Partners
                    .Select(p => new
                    {
                        PartnerId = p.PartnerId,
                        Name = p.PartnerName,
                        ContactInfo = p.ContactInfo
                    })
                    .ToList();

                return Results.Json(partners);
            });

            app.MapPost("/partnersupdate", async (PartnerDto partnerData) =>
            {
                if (partnerData == null)
                    return Results.BadRequest("Неверные данные");

                using var db = new PostgresContext();
                var partner = await db.Partners.FirstOrDefaultAsync(p => p.PartnerId == partnerData.PartnerId);

                if (partner == null)
                    return Results.NotFound("Партнёр не найден");

                partner.PartnerName = partnerData.Name;
                partner.ContactInfo = partnerData.ContactInfo;

                await db.SaveChangesAsync();
                return Results.Ok("Обновление успешно");
            });

            app.MapGet("/stores", () =>
            {
                using var db = new PostgresContext();
                var stores = db.Stores
                    .Select(s => new
                    {
                        StoreId = s.StoreId,
                        Address = s.StoreAddress
                    })
                    .ToList();

                return Results.Json(stores);
            });

            app.MapPost("/storesupdate", async (StoreDto storeData) =>
            {
                if (storeData == null)
                    return Results.BadRequest("Неверные данные");

                using var db = new PostgresContext();
                var store = await db.Stores.FirstOrDefaultAsync(s => s.StoreId == storeData.StoreId);

                if (store == null)
                    return Results.NotFound("Магазин не найден");

                store.StoreAddress = storeData.Address;

                await db.SaveChangesAsync();
                return Results.Ok("Обновление успешно");
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

    public class WarehouseItem
    {
        public int Id { get; set; } // Не используется напрямую на сервере, но может быть нужен на клиенте
        public string StoreName { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }
    public class ClientDto
    {
        public int ClientId { get; set; } // ClientId
        public string Name { get; set; }
        public string ContactInfo { get; set; }
    }
    public class EmployeeDto
    {
        public int EmployeeId { get; set; } // EmployeeId
        public string Name { get; set; }
        public int StoreId { get; set; } // Используется только при отображении
        public string StoreName { get; set; }
        public string ContactInfo { get; set; }
    }

    public class PartnerDto
    {
        public int PartnerId { get; set; } // PartnerId
        public string Name { get; set; }
        public string ContactInfo { get; set; }
    }

    public class StoreDto
    {
        public int StoreId { get; set; } // StoreId
        public string Address { get; set; }
    }

}
