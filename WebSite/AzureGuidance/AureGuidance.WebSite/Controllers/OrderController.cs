﻿using AzureGuidance.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.ServiceBus.Messaging;
using AzureGuidance.Domain;
using System.Threading.Tasks;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace AzureGuidance.Web.Controllers
{
   
    public class OrderController : Controller
    {
        AzureDocumentDBHelper azureDocDBHelper;
        IDatabase cache;
        public OrderController()
        {
            try
            {
                string endpointUrl = ConfigurationManager.AppSettings["Microsoft.DocumentDB.StorageUrl"];
                string authorizationKey = ConfigurationManager.AppSettings["Microsoft.DocumentDB.AuthorizationKey"];
                azureDocDBHelper = new AzureDocumentDBHelper(endpointUrl, authorizationKey);
                string redisConnString = ConfigurationManager.AppSettings["Microsoft.RedisCache.Connection"];
                if (null != redisConnString)
                {
                    ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(redisConnString);
                    cache = connection.GetDatabase();
                }
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public async Task<ActionResult> DisplayProducts()
        {
            List<Product> lstProducts = null;
            ProductOrder productOrder = new ProductOrder();
            List<AddProduct> lstAddProducts = new List<AddProduct>();

            string value = cache.StringGet("Products");
            if (null != value)
            {
                lstProducts = JsonConvert.DeserializeObject<List<Product>>(value);
            }
            else
            {
                lstProducts = await azureDocDBHelper.GetProducts();
                value = JsonConvert.SerializeObject(lstProducts);
                cache.StringSet("Products", value);
            }
            foreach (Product pd in lstProducts)
            {
                AddProduct addPd = new AddProduct();
                addPd.ProductId = pd.ProductId;
                addPd.ProductName = pd.ProductName;
                addPd.UnitPrice = pd.UnitPrice;
                lstAddProducts.Add(addPd);
            }
            productOrder.lstProducts = lstAddProducts;
            return View(productOrder);
        }
        public async Task<ActionResult> DisplayProductsForAdd()
        {
            List<Product> lstProducts = null;
            string value = cache.StringGet("Products");
            if (null != value)
            {
                lstProducts = JsonConvert.DeserializeObject<List<Product>>(value);
            }
            else
            {
                lstProducts = await azureDocDBHelper.GetProducts();
                value = JsonConvert.SerializeObject(lstProducts);
                cache.StringSet("Products", value);
            }

            return View("Products", lstProducts);
        }

        [HttpPost]
        public ActionResult SubmitOrder(ProductOrder productOrder)
        {

            //if (ModelState.IsValid)
            {
                try
                {
                    ViewBag.SubmitResponse = string.Empty;
                    AzureGuidance.Domain.Order order = new AzureGuidance.Domain.Order();
                    order.CustomerName = productOrder.order.CustomerName;
                    order.OrderDate = DateTime.Now;
                    order.EmailId = productOrder.order.EmailId;
                    order.ProductOrderDetailsList = new List<ProductDetails>();
                    foreach (AddProduct p in productOrder.lstProducts)
                    {
                        if (true == p.SelectProduct)
                        {
                            ProductDetails productDetails = new ProductDetails();
                            productDetails.ProductName = p.ProductName;
                            productDetails.ProductQuantity = p.ProductQuantity;
                            order.TotalDue += p.UnitPrice * p.ProductQuantity;
                            order.orderStatus = "Processed";
                            order.OrderId  = Guid.NewGuid();
                            order.ProductOrderDetailsList.Add(productDetails);
                            p.ProductQuantity = 0;
                            p.SelectProduct = false;
                        }

                    }
                    var message = new Microsoft.ServiceBus.Messaging.BrokeredMessage(order);
                    if (null == ServiceBusTopicHelper.OrdersTopicClient)
                    {
                        ServiceBusTopicHelper.Initialize();
                    }
                    ServiceBusTopicHelper.OrdersTopicClient.Send(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            ProductOrder pOrder = new ProductOrder();
            List<AddProduct> lstProducts = new List<AddProduct>();

            pOrder.lstProducts = productOrder.lstProducts;
            ModelState.Clear();
            ViewBag.SubmitResponse = "Order proccessed successfully.";
            return View("DisplayProducts", pOrder);
        }
        private void GetProducts(List<AddProduct> productLst)
        {
            foreach(AddProduct p in productLst)
            {
                //p.ProductQuantity = "";
                p.SelectProduct = false;
            }
        }

        // Get: /Order/Delete/DisplayProductsOfOrder/4f2fa5fc-1809-4712-8b88-71761315a271
        public async Task<ActionResult> DisplayProductsOfOrder(Guid id )
        {
            List<ProductDetails> listProductDetails;
            listProductDetails = await azureDocDBHelper.GetOrderProductsDetails(id);
           return PartialView("_CustomerOrder", listProductDetails);
           
        }
        

        public async Task<ActionResult> ListOrder()
        {
            //OrderDetails orderDetails = new OrderDetails();
            List<AzureGuidance.Domain.Order> listOrderDetails;
            listOrderDetails = await azureDocDBHelper.GetOrders();
            return View(listOrderDetails);
        }
    
        public async Task<ActionResult> AddProduct(string productName, string unitPrice)
        {
            Product prod = new Product();
            prod.ProductId = new Guid().ToString();
            prod.ProductName = productName;
            decimal price;
            decimal.TryParse(unitPrice, out price);
            prod.UnitPrice = price;
            await azureDocDBHelper.AddDocument(prod, "Products");

            List<Product> lstProducts;
            lstProducts = await azureDocDBHelper.GetProducts();
            
            string value = JsonConvert.SerializeObject(lstProducts);
            cache.StringSet("Products", value);
            return View("Products", lstProducts);
        }
        
    }
}