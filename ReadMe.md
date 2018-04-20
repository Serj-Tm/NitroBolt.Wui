# NitroBolt.Wui
C# Server-Side React

## Создание проекта
1. Создать новый проект Asp.net с поддержкой Web Api 2 `New Project -> Visual C# -> Windows -> Web -> Asp.net Web Application:  Asp.net 4.5 Templates -> Empty (Web Api: checked)`
2. Install-Package NitroBolt.Wui
3. Добавить Controller, отнаследованный от ApiController
4. Добавить метод View с сигнатурой `static HtmlResult<HElement> View(MainState state, JsonData[] commands, HttpRequestMessage request)`
5. Добавить Route

    [HttpGet, HttpPost]
    [Route(<Название>)]
    public HttpResponseMessage Route()
    {
        return HWebApiSynchronizeHandler.Process<object>(this.Request, View);
    }

## Перенос проекта с NitroBolt.Wui 1.x
1. Скопировать каждый класс View
2. Переименовать в Controller и отнаследовать от ApiController
3. Поменять тип в HView с HttpContent на HttpRequestMessage
4. Добавить Route

    [HttpGet, HttpPost]
    [Route(<Название>)]
    public HttpResponseMessage Route()
    {
        return HWebApiSynchronizeHandler.Process<object>(this.Request, HView);
    }
5. В javascript-обработчиках событий заменить "this[0]"/"this.get(0)" на "this". Начиная с NitroBolt.Wui 2.0 в обработчики событий передается ссылка на HtmlElement, ранее передавался HtmlElement обернутый в JQuery.
