/// <reference path="ts/jquery.d.ts" />


interface ServerEvent
{
  (json: string): void;
}

class ContainerSynchronizer
{
  constructor(container:JQuery = null, name:string = null, sync_refresh_period:number = 10 * 1000)
  {
    this.container = container != null ? $(container) : $('body');
    //this.server_event = server_event == null ? this.server_web_event : server_event;
    this.container_name = name;
    this.sync_refresh_period = sync_refresh_period;
    this.id = Math.random().toString();

    window.setInterval(() => this.update_all(), this.sync_refresh_period);
    window.setInterval(() =>
      {
        if(this.is_need_update)
        {
          this.is_need_update = false;
          this.update_all();
        }
      }
      , 50);
    this.update_all();
  }
  container: JQuery;
  container_name: string;
  //server_event: ServerEvent;
  sync_refresh_period: number;
  id: string;



  static eventProps = ['type', 'bubbles', 'cancelable', 'eventPhase', 'timeStamp',
      'button', 'clientX', 'clientY', 'screenX', 'screenY',
      'keyIdentifier', 'keyLocation', 'keyCode', 'charCode', 'which',
      'altKey', 'ctrlKey', 'metaKey', 'shiftKey'
  ];
  server_element_event(_element: any, event: JQueryEventObject, data?: {}):void
  {
    var element = $(_element);
    var e = null;
    if (event != null)
    {
      e = {};
      var eventProps = ContainerSynchronizer.eventProps;
      for (var i:number = 0; i < eventProps.length; ++i)
          e[eventProps[i]] = event[eventProps[i]];
    }
    var element_data = element.data();
    var result_data = {};
    if (element_data.container != null) 
    {
      var container = null;
      var parents = element.parents();
      for (var i: number = 0; i < parents.length; ++i)
      {
        if ($(parents[i]).data().name == element_data.container)
        {
            container = $(parents[i]);
            break;
        }
      }
      if (container != null)
      {
        result_data = $.extend(result_data, container.data());
        var childs = $.merge(container.find('input'), container.find('select'));
        childs = $.merge(childs, container.find('textarea'));
        for (var i: number = 0; i < childs.length; ++i)
        {
          var child = $(childs[i]);
          if (child.data().name != null)
          {
              result_data[child.data().name] = child.is(':checkbox') ? child.is(':checked') : child.val();
          }
        }
      }
    }
    this.server_event(JSON.stringify({ value: element.is(':checkbox') ? element.is(':checked') : element.val(), checked: element.is(':checked'), data: $.extend(result_data, element_data, data), event: e }));
  }
  find_element(current:JQuery, path:PathEntry[]):JQuery
  {
    var len = path.length;
    for (var i: number = 0; i < len; ++i)
    {
      if (!current)
          return null;
      var pentry = path[i];
        //    $('#log').append('find_element: ' +  i + ' ' + pentry.index);
      if (pentry.kind == 'element')
      {
        current = current.children().eq(pentry.index);
        //    $('#log').append('current_element: ' +  current);
      }
    }
    return current;
  }
  is_event_name(name:string):boolean
  {
    if (!(name.substring(0, 2) === 'on'))
        return false;
    switch (name)
    {
        case 'onclick':
        case 'ondblclick':
        case 'onmousedown':
        case 'onmousemove':
        case 'onmouseover':
        case 'onmouseout':
        case 'onmouseup':
        case 'onkeydown':
        case 'onkeypress':
        case 'onkeyup':

        case 'onblur':
        case 'onchange':
        case 'onfocus':
        case 'onreset':
        case 'onselect':
        case 'onsubmit':

        case 'onabort':
        case 'onerror':
        case 'onload':
        case 'onresize':
        case 'onscroll':
        case 'onunload':
            return true;
    }
    return false;
  }
  event_on(element:JQuery, event:string, value:string): void
  {
    if (value != null)
    {
      element.on(event, e =>
        {
          if (value.substr(0, 2) == ';;')
          {
            var res = function (sync, e) { return eval(value); }.apply(element, [this, e]);
            if (typeof (res) == 'boolean')
              return res;
          }
          else
          {
            var res = function(){return eval(value);}.apply(element);
            if (typeof (res) == 'boolean')
                return res;
            this.server_element_event(element, e);
          }
        }
      );
    }
  }
  set_element(element:JQuery, desc:ElementDescription):void
  {
    if (!desc || !element)
        return;

    var len = !desc.e ? 0 : desc.e.length;
    for (var i: number = 0; i < len; ++i)
    {
        //window.external.Debug(element[0].tagName != null ? element[0].tagName:element);
      element.append(this.create_element(desc.e[i]));
    }
    //window.external.Debug('attrs');
    var len = !desc.a ? 0 : desc.a.length;
    for (var i: number = 0; i < len; ++i)
    {
      if (this.is_event_name(desc.a[i].name))
      {
        var event = desc.a[i].name.substring(2);
        var value = desc.a[i].value;
        element.off(event);
        this.event_on(element, event, value);
      }
      else if (desc.a[i].name.substring(0, 5) === 'data-')
      {
        element.data(desc.a[i].name.substring(5), desc.a[i].value);
      }
      else
      {
        element.attr(desc.a[i].name, desc.a[i].value);
      }
    }
    //window.external.Debug('text');
    if (desc.t != null)
    {
      element.text(desc.t.value);
    }
    if (desc.h != null)
    {
      element.html(desc.h);
    }
  }

  create_element(desc:ElementDescription):JQuery
  {
    var element = $(desc.ns ? document.createElementNS(desc.ns, desc.name) : document.createElement(desc.name));
    //window.external.Debug('create_element: ' + desc.a.length);
    var jsInit:string = null;
    for (var i:number = 0; i < (!desc.a ? 0 : desc.a.length); ++i) 
    {
      //window.external.Debug('n: ' + desc.a[i].name);
      if (desc.a[i].name == 'js-init')
          jsInit = desc.a[i].value;
    }
    if (jsInit != null)
    {
      //window.external.Debug('js-init: ' + jsInit);
      !(function () { return eval(jsInit); }.apply(element));

      //var _this = element;
      //eval(jsInit);
    }
    this.set_element(element, desc);
    return element;
  }

  change_element(current: JQuery, cmd: string, desc: ElementDescription):void
  {
    if (!current)
      return;
    switch (cmd)
    {
      case 'remove':
        current.remove();
        break;
      case 'clear':
        current.empty();
        break;
      case 'clear-all':
        current.empty();
        var attributes = $.map(current[0].attributes, item => item.name);

        $.each(attributes, (i, item) => current.removeAttr(item));
        break;
      case 'set':
        this.set_element(current, desc);
        break;
      case 'after':
        current.after(this.create_element(desc));
        break;
      case 'insert':
        current.prepend(this.create_element(desc));
        break;
      case 'js-update':
        !(function () { return eval(<string><any>desc); }.apply(current));
        //var _this = current;
        //eval(<string><any>desc);
        break;

    }
  }

  apply_commands(commands: Command[]):void
  {
    var len = commands.length;
    for (var i: number = 0; i < len; ++i)
    {
      var command = commands[i];
      this.change_element(this.find_element(this.container, command.path), command.cmd, command.value);
    }
  }
  sync(data): void
  {
    if (data.prev_cycle == this.cycle && !this.is_updating)
    {
      this.is_updating = true;
      try
      {
        this.apply_commands(data.updates);
        this.cycle = data.cycle;
        this.commands = this.commands.slice(data.processed_commands != null ? data.processed_commands : 0);
        if (this.commands.length > 0)
          this.is_need_update = true;
      }
      finally
      {
        this.is_updating = false;
      }
    }
    else
    {
      this.is_need_update = true;
    }
  }

  cycle: number = 0;
  is_need_update: boolean = false;
  is_updating: boolean = false;

  commands: string[] = []; 

  server_event(json:string):void
  {
    this.commands.push(json);
    $.post(this.js_path() + '?cycle=' + this.cycle, { 'frame-id': this.id, 'cycle': this.cycle, 'commands': this.commands }, data => this.sync(data), 'json');
  }

  update_all():void
  {
    try
    {
      if (this.commands.length > 0)
      {
        $.post(this.js_path() + '?cycle=' + this.cycle, { 'frame-id': this.id, 'cycle': this.cycle, 'commands': this.commands }, data => this.sync(data), 'json');
      }
      else
      {      
        $.getJSON(this.js_path() + '?cycle=' + this.cycle + '&r=' + (1000 * Math.random() + '').substring(0, 3), data => this.sync(data));
      }
    }
    catch (e) { }
  }
  js_path(): string
  {
    if (this.container_name != null)
      return this.container_name;
    return JsPath();
  }
}

function JsPath():string
{
  var pathname = window.location.pathname;
  if (pathname == '/')
    pathname = '/default.html';
  if (pathname[pathname.length - 1] == '/')
    pathname = pathname.substring(0, pathname.length - 1);
  return (<any>window.location).origin + pathname + '.js';
}

//function init_sync_container(container)
//{
//  window.setInterval(function () { update_container(container); }, (typeof sync_refresh_period === 'undefined') ? 60 * 1000 : sync_refresh_period);
//  update_container(container);
//}
//function sync_container_from_json(container: JQuery, json:string)
//{
//  sync_container(container, $.parseJSON(json));
//  return true;
//}


class Command
{
  cmd: string;
  path: PathEntry[];
  value: ElementDescription; //TODO сейчас тип ElementDescription | string; Для единообразия у команды js-update поменять тип тоже на ElementDescription, добавив поле j
}
class ElementDescription
{
  ns: string;
  name: string;
  a: AttributeDescription[];
  e: ElementDescription[];
  t: TextDescription;
  h: string;
}
class AttributeDescription
{
  name: string;
  value: string;
}
class TextDescription
{
  value: string;
}

class PathEntry
{
  kind: string;
  index: number;
}
