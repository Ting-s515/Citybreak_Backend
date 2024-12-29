using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using testCitybreak.DTO;
using testCitybreak.Models;
namespace testCitybreak.Controllers
{
	public class CitybreakController : ControllerBase
	{
		private readonly CitybreakContext _context;
		private readonly PasswordHasher<memberTable> _passwordHasher;
		private readonly ILogger<CitybreakController> _logger;
		public CitybreakController(CitybreakContext context, ILogger<CitybreakController> logger)
		{
			_context = context;
			_passwordHasher = new PasswordHasher<memberTable>();
			_logger = logger;
		}

		[HttpPost("getProducts")]
		public async Task<IActionResult> GetProducts([FromBody] product_classification value)
		{
			_logger.LogInformation("收到商品分類參數={}", value.classification);
			try
			{
				List<productTable> products = await (from x in _context.product_classification
													 where x.classification == value.classification.Trim()
													 join y in _context.productTable on x.classificationID equals y.classificationID
													 select y).ToListAsync();
				return Ok(new
				{
					success = true,
					message = "抓取商品成功",
					data = products,
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message = "抓取商品失敗",
					error = ex.Message,
				});
			}
		}
		[HttpPost("getOrders")]
		public async Task<IActionResult> GetOrders([FromBody] orderTable value)
		{
			_logger.LogInformation("收到請求={}", value.userID);
			if (value.userID == 0)
			{
				return BadRequest(new { success = false, message = "userID 不正確" });
			}
			try
			{
				List<OrderDataDTO> orders = await _context.orderTable
					.Where(x => x.userID == value.userID && x.orderStatus == "已付款")
					.Select(o => new OrderDataDTO
					{
						orderID = o.orderID,
						merchantTradeNo = o.merchantTradeNo,
						orderTime = o.orderTime,
						totalPrice = o.totalPrice,
						orderStatus = o.orderStatus,
					}).ToListAsync();
				if (!orders.Any())
				{
					_logger.LogError("找不到訂單");
					return NotFound(new
					{
						success = false,
						message = "找不到訂單"
					});
				}
				return Ok(new
				{
					success = true,
					message = "抓取訂單成功",
					data = orders,
				});
			}
			catch (Exception ex)
			{
				_logger.LogError("錯誤={} 堆疊資訊={}", ex.Message, ex);
				return StatusCode(500, new
				{
					success = false,
					message = "抓取訂單失敗",
					error = ex.Message,
				});
			}
		}
		[HttpPost("getOrderDetail")]
		public async Task<IActionResult> GetOrderDetail([FromBody] orderTable value)
		{
			_logger.LogInformation("收到訂單編號={}", value.merchantTradeNo);
			try
			{
				List<OrderDataDTO> result = await (from order in _context.orderTable
												   where order.merchantTradeNo == value.merchantTradeNo
												   join orderDetail in _context.order_details on order.orderID equals orderDetail.orderID
												   join product in _context.productTable on orderDetail.productID equals product.productID
												   select new OrderDataDTO
												   {
													   merchantTradeNo = order.merchantTradeNo,
													   orderTime = order.orderTime,
													   totalPrice = order.totalPrice,
													   quantity = orderDetail.quantity,
													   productName = product.productName,
													   imagePath = product.imagePath,
													   unitPrice = product.unitPrice,
												   }).ToListAsync();
				if (!result.Any())
				{
					return NotFound(new { success = false, message = "找不到訂單明細" });
				}
				OrderDataDTO? orderData = result.FirstOrDefault();
				return Ok(new
				{
					success = true,
					message = "抓取訂單明細成功",
					data = new
					{
						orderData.merchantTradeNo,
						orderData.orderTime,
						orderData.totalPrice,
						products = result.Select(x => new
						{
							x.productName,
							x.unitPrice,
							x.quantity,
							x.imagePath,
						})
					}
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message = "抓取訂單明細失敗",
					error = ex.Message,
				});
			}
		}
		[HttpPost("searchProducts")]
		public async Task<IActionResult> SearchProducts([FromBody] productTable product)
		{
			_logger.LogInformation("接收到的參數: {}", product.productName);
			try
			{
				List<productTable> products = await _context.productTable.Where(x => x.productName.Contains(product.productName.Trim()))
				.ToListAsync();
				if (!products.Any())
				{
					return NotFound(new
					{
						success = false,
						message = "查無結果"
					});
				}
				return Ok(new
				{
					success = true,
					data = products
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message = "發生錯誤",
					error = ex.Message
				});
			}
		}
	}
}
