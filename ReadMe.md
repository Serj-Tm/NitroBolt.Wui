# NitroBolt.Wui
C# Server-Side React

## Создание проекта
1. Создать новый проект Asp.net с поддержкой Web Api 2
    
  New Project -> Visual C# -> Windows -> Web -> Asp.net Web Application:  Asp.net 4.5 Templates -> Empty (Web Api: checked)
2. Install-Package NitroBolt.Wui

## Перенос проекта с NitroBolt.Wui 1.x
1. Каждый класс View

   1. Скопировать
   2. Переименовать в Controller и отнаследовать от ApiController
   3. Поменять тип в HView с HttpContent на HttpRequestMessage
   4. Добавить Route

        [HttpGet, HttpPost]
        [Route(<Название>)]
        public HttpResponseMessage Route()
        {
            return HWebApiSynchronizeHandler.Process<object>(this.Request, HView);
        }
