using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NitroBolt.Wui
{
  class EventTest
  {
    public static void AddEventHandler(EventInfo eventInfo, object item, Action<object, EventArgs> action)
    {
      var parameters = eventInfo.EventHandlerType
        .GetMethod("Invoke")
        .GetParameters()
        .Select(parameter => Expression.Parameter(parameter.ParameterType))
        .ToArray();

      var invoke = action.GetType().GetMethod("Invoke");

      var handler = Expression.Lambda(
          eventInfo.EventHandlerType,
          Expression.Call(Expression.Constant(action), invoke, parameters[0], parameters[1]),
          parameters
        )
        .Compile();

      eventInfo.AddEventHandler(item, handler);
    }
  }
}
