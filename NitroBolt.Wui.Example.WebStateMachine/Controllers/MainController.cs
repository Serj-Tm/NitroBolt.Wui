using NitroBolt.Functional;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace NitroBolt.Wui.Example.WebStateMachine.Controllers
{
    public class MainController:ApiController
    {
        [HttpGet, HttpPost]
        [Route("")]
        public HttpResponseMessage Route()
        {
            return HWebApiSynchronizeHandler.Process<MainState>(this.Request, HView);
        }

        public static NitroBolt.Wui.HtmlResult<HElement> HView(MainState state, JsonData[] jsons, HttpRequestMessage request)
        {

            foreach (var json in jsons.OrEmpty())
            {
                switch (json.JPath("data", "command")?.ToString())
                {
                    case "new-order":
                        {
                            var name = json.JPath("data", "name")?.ToString() ?? "C";
                            var isDelivery = json.JPath("data", "is-delivery")?.ToString() == "True";
                            var toTime = ToTime(json.JPath("data", "to-time")?.ToString());
                            var status = toTime == null ? ProductStatus.New : ProductStatus.InQueue;
                            var products = name == "B" ? new[] { new Product("K", status) } : new[] { new Product("M", status), new Product("P", status) };
                            state = state.With(orders: state.Orders.Add(new Order(name: name, isDelivery: isDelivery, status: toTime == null ? OrderStatus.New : OrderStatus.InQueue, products: ImmutableArray.Create(products), toTime: toTime)));
                        }
                        break;
                    case "product-prepare":
                        {
                            var orderId = ConvertHlp.ToGuid(json.JPath("data", "order"));
                            var productId = ConvertHlp.ToGuid(json.JPath("data", "product"));
                            var order = state.Orders.FirstOrDefault(_order => _order.Id == orderId);
                            var product = order?.Products.OrEmpty().FirstOrDefault(_product => _product.Id == productId);
                            if (product != null && product.Status != ProductStatus.New)
                                product = null;
                            if (product != null)
                            {
                                state = state.With(orders: state.Orders.Replace(order, order.With(products: order.Products.Replace(product, product.With(status: ProductStatus.Prepare)))));
                            }
                        }
                        break;
                    case "product-ready":
                        {
                            var orderId = ConvertHlp.ToGuid(json.JPath("data", "order"));
                            var productId = ConvertHlp.ToGuid(json.JPath("data", "product"));
                            var order = state.Orders.FirstOrDefault(_order => _order.Id == orderId);
                            var product = order?.Products.OrEmpty().FirstOrDefault(_product => _product.Id == productId);
                            if (product != null && product.Status != ProductStatus.Prepare)
                                product = null;
                            if (product != null)
                            {
                                state = state.With(orders: state.Orders.Replace(order, order.With(products: order.Products.Replace(product, product.With(status: ProductStatus.Ready)))));
                            }
                        }
                        break;
                    case "order-build":
                        {
                            var orderId = ConvertHlp.ToGuid(json.JPath("data", "order"));
                            var order = state.Orders.FirstOrDefault(_order => _order.Id == orderId);
                            if (order != null && order.Status != OrderStatus.Prepare && !order.IsReady)
                                order = null;
                            if (order != null)
                            {
                                state = state.With(orders: state.Orders.Replace(order, order.With(status: OrderStatus.Ready)));
                            }
                        }
                        break;
                    case "courier":
                        {
                            var orderId = ConvertHlp.ToGuid(json.JPath("data", "order"));
                            var courier = json.JPath("data", "courier")?.ToString();
                            var order = state.Orders.FirstOrDefault(_order => _order.Id == orderId);
                            if (order != null && order.Status != OrderStatus.Ready && !order.IsDelivery)
                                order = null;
                            if (order != null)
                            {
                                state = state.With(orders: state.Orders.Replace(order, order.With(status: OrderStatus.ToDelivery, courier: courier)));
                            }
                        }
                        break;
                    case "order-deliveried":
                        {
                            var orderId = ConvertHlp.ToGuid(json.JPath("data", "order"));
                            var order = state.Orders.FirstOrDefault(_order => _order.Id == orderId);
                            if (order != null && order.Status != OrderStatus.ToDelivery)
                                order = null;
                            if (order != null)
                            {
                                state = state.With(orders: state.Orders.Replace(order, order.With(status: OrderStatus.Deliveried)));
                            }
                        }
                        break;
                    case "order-to-table":
                        {
                            var orderId = ConvertHlp.ToGuid(json.JPath("data", "order"));
                            var order = state.Orders.FirstOrDefault(_order => _order.Id == orderId);
                            if (order != null && order.Status != OrderStatus.Ready && order.IsDelivery)
                                order = null;
                            if (order != null)
                            {
                                state = state.With(orders: state.Orders.Replace(order, order.With(status: OrderStatus.ToTable)));
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            state = state.With(orders: DeQueue(state.Orders, DateTime.Now));

            var page = Page(state);
            return new NitroBolt.Wui.HtmlResult<HElement>
            {
                Html = page,
                State = state,
            };
        }
        static ImmutableList<Order> DeQueue(ImmutableList<Order> orders, DateTime currentTime)
        {
            return orders.Select(order => order.Status == OrderStatus.InQueue && order.ToTime <= currentTime ? order.With(status: OrderStatus.New, products: order.Products.Select(product => product.With(status: ProductStatus.New)).ToImmutableArray()) : order).ToImmutableList();
        }

        static DateTime? ToTime(string text)
        {
            DateTime time;
            if (DateTime.TryParse(text, out time))
                return time;
            return null;
        }
        private static HElement Page(MainState state)
        {
            var page = h.Html
            (
              h.Head(
                h.Element("title", "NitroBolt.Wui - WebExample. State machine"),
                h.Css
                (
                  @"
              .section {min-height:100px;border:1px solid lightgray;margin-bottom:2px;}
              .header {background-color:#E0E0FF;}
            "
                )
              ),
              h.Body
              (
                 h.Div
                 (
                   h.@class("section"),
                   h.Div(h.@class("header"), "Клиентский интерфейс"),
                   h.Div
                   (
                     h.data("name", "order"), h.data("id", state.Orders.Count),
                     h.Div("Новый заказ"),
                     h.Div("Опции"),
                     h.Span("\xA0▪ "), h.Span("На указанное время: "),
                     h.Input(h.type("text"), h.data("name", "to-time")), h.Span(h.style("color:darkgray;"), $" Пример(+15с): {DateTime.Now.AddSeconds(15)}"),
                     h.Br(),
                     h.Span("\xA0▪ "), h.Input(h.type("checkbox"), h.data("name", "is-delivery")), h.Span("Доставка"), h.Br(),
                     new[] { "A", "B" }
                      .Select(order =>
                        h.Input(h.type("button"), new hdata { { "command", "new-order" }, { "container", "order" }, { "name", order } }, h.value("Сделать заказ типа " + order), h.onclick(";"))
                      )
                   ),
                   h.Div
                   (
                     h.Div("Заказы"),
                     state.Orders.Where(order => order.Status != OrderStatus.ToTable && order.Status != OrderStatus.Deliveried).Select(order =>
                       h.Div
                       (
                         h.Span(order.Title), ViewStatus(order.Status), order.Status == OrderStatus.InQueue ? h.Span(string.Format(" Отложен до {0:dd.MM.yy HH:mm}", order.ToTime)) : null,
                         order.IsDelivery ? h.Span(h.title("Доставка"), " => ") : null//,

                       ))
                   )
                 ),
                 h.Div
                 (
                   h.@class("section"),
                   h.Div(h.@class("header"), "Производственный интерфейс"),
                   state.Orders.Where(order => order.Status == OrderStatus.New || order.Status == OrderStatus.Prepare && !order.IsReady).Select(order =>
                     h.Div
                     (
                       h.Div(h.Span("Заказ: "), h.Span(order.Title), ViewStatus(order.Status)),
                       order.Products.Select(product =>
                         h.Div
                         (
                           h.style("padding-left:15px;"),
                           h.Span("Продукт " + product.Name), ViewStatus(product.Status),
                           product.Status == ProductStatus.New || product.Status == ProductStatus.Prepare
                           ? h.Input(h.type("button"), new hdata { { "command", product.Status == ProductStatus.New ? "product-prepare" : "product-ready" }, { "order", order.Id }, { "product", product.Id } }, product.Status == ProductStatus.New ? h.value("Приготовить") : h.value("Готово"), h.onclick(";"))
                           : null
                         )
                       )
                     )

                   )
                 ),
                 h.Div
                 (
                   h.@class("section"),
                   h.Div(h.@class("header"), "Интерфейс сборщика"),
                   state.Orders.Where(order => order.IsReady).Select(order =>
                     h.Div
                     (
                       h.Span(order.Title),
                       h.Span(" "),
                       h.Input(h.type("button"), new hdata { { "command", "order-build" }, { "order", order.Id } }, h.value("Собрать"), h.onclick(";"))
                     )

                   )
                 ),
                 h.Div
                 (
                   h.@class("section"),
                   h.Div(h.@class("header"), "Интерфейс официанта"),
                   state.Orders.Where(order => order.Status == OrderStatus.Ready && !order.IsDelivery).Select(order =>
                     h.Div
                     (
                       h.Span(order.Title),
                       h.Span(" "),
                       h.Input(h.type("button"), new hdata { { "command", "order-to-table" }, { "order", order.Id } }, h.value("Передать клиенту"), h.onclick(";"))
                     )

                   )
                 ),
                 h.Div
                 (
                   h.@class("section"),
                   h.Div(h.@class("header"), "Назначить курьера"),
                   state.Orders.Where(order => order.Status == OrderStatus.Ready && order.IsDelivery).Select(order =>
                     h.Div
                     (
                       h.Span(order.Title),
                       h.Span(" "),
                       new[] { "Иванов П.", "Петров Д.", "Сидоров К." }
                       .Select(courier =>
                         h.Input(h.type("button"), new hdata { { "command", "courier" }, { "order", order.Id }, { "courier", courier } }, h.value(courier), h.onclick(";"))
                       )
                     )

                   )
                 ),
                 h.Div
                 (
                   h.@class("section"),
                   h.Div(h.@class("header"), "Интерфейс курьера"),
                   state.Orders.Where(order => order.Status == OrderStatus.ToDelivery).Select(order =>
                     h.Div
                     (
                       h.Span(order.Title),
                       h.Span(" "),
                       h.Input(h.type("button"), new hdata { { "command", "order-deliveried" }, { "order", order.Id } }, h.value("Передать клиенту"), h.onclick(";"))
                     )

                   )
                 ),
                 h.Div
                 (
                   h.@class("section"),
                   h.Div(h.@class("header"), "Выполненные заказы"),
                   state.Orders.Where(order => order.Status == OrderStatus.ToTable || order.Status == OrderStatus.Deliveried).Select(order =>
                     h.Div
                     (
                       h.Span(order.Title)
                     )

                   )
                 )

              )
            );
            return page;
        }



        static readonly HBuilder h = null;

        static HElement ViewStatus(OrderStatus status)
        {
            return h.Span(h.style("color:darkgray;font-size:85%;"), string.Format(" ({0}) ", DisplayStatus(status)));
        }
        static HElement ViewStatus(ProductStatus status)
        {
            return h.Span(h.style("color:darkgray;font-size:85%;"), string.Format(" ({0}) ", DisplayStatus(status)));
        }
        static string DisplayStatus(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.InQueue:
                    return "В очереди";
                case OrderStatus.New:
                    return "Новый";
                case OrderStatus.Prepare:
                    return "Приготовление";
                case OrderStatus.Ready:
                    return "Собран";
                case OrderStatus.ToDelivery:
                    return "Передан курьеру";
                case OrderStatus.Deliveried:
                    return "Доставлен";
                case OrderStatus.ToTable:
                    return "Передан в зал";
            }
            return null;
        }
        static string DisplayStatus(ProductStatus status)
        {
            switch (status)
            {
                case ProductStatus.InQueue:
                    return "В очереди";
                case ProductStatus.New:
                    return "Новый";
                case ProductStatus.Prepare:
                    return "Приготовление";
                case ProductStatus.Ready:
                    return "Готов";
            }
            return null;
        }
    }
    public class MainState
    {
        public MainState() : this(null) { }
        public MainState(ImmutableList<Order> orders = null)
        {
            Orders = orders ?? ImmutableList<Order>.Empty;
        }
        public readonly ImmutableList<Order> Orders;
        public MainState With(ImmutableList<Order> orders = null)
        {
            return new MainState(orders ?? Orders);
        }
    }
    public class Order
    {
        public Order(string name, DateTime? time = null, DateTime? toTime = null, OrderStatus? status = null, ImmutableArray<Product>? products = null, bool isDelivery = false, string courier = null, Guid? id = null)
        {
            this.Name = name;
            this.Time = time ?? DateTime.Now;
            this.ToTime = toTime;
            this.Id = id ?? Guid.NewGuid();
            this.IsDelivery = isDelivery;
            Products = products ?? ImmutableArray.Create<Product>();
            this.Status = status ?? OrderStatus.New;
            if (this.Status == OrderStatus.New && Products.Any(_product => _product.Status == ProductStatus.Prepare))
                this.Status = OrderStatus.Prepare;

            this.Courier = courier;
            //if (this.Status == OrderStatus.Prepare && Products.All(_product => _product.Status == ProductStatus.Ready))
            //  this.Status = OrderStatus.Ready;
        }
        public readonly string Name;
        public readonly DateTime Time;
        public readonly DateTime? ToTime;
        public string Title { get { return string.Format("{0}{1:HH:mm:ss.f}", Name, Time); } }
        public readonly OrderStatus Status;
        public bool IsReady { get { return this.Status == OrderStatus.Prepare && Products.All(_product => _product.Status == ProductStatus.Ready); } }
        public readonly Guid Id;
        public readonly ImmutableArray<Product> Products;
        public readonly bool IsDelivery;
        public readonly string Courier;

        public Order With(string name = null, DateTime? time = null, DateTime? toTime = null, OrderStatus? status = null, ImmutableArray<Product>? products = null, bool? isDelivery = null, Option<string> courier = null, Guid? id = null)
        {
            return new Order(name ?? this.Name, time ?? this.Time, toTime ?? this.ToTime, status ?? this.Status, products ?? this.Products, isDelivery ?? this.IsDelivery, courier.Else(this.Courier), id ?? this.Id);
        }
    }
    public enum OrderStatus
    {
        InQueue,
        New,
        Prepare,
        Ready,
        ToDelivery,
        Deliveried,
        ToTable,
    }
    public class Product
    {
        public Product(string name, ProductStatus status, Guid? id = null)
        {
            this.Id = id ?? Guid.NewGuid();
            this.Name = name;
            this.Status = status;
        }
        public readonly Guid Id;
        public readonly string Name;
        public readonly ProductStatus Status;

        public Product With(string name = null, ProductStatus? status = null, Guid? id = null)
        {
            return new Product(name ?? this.Name, status ?? Status, id ?? Id);
        }
    }
    public enum ProductStatus
    {
        InQueue,
        New,
        Prepare,
        Ready,
    }
}