# NitroBolt.Wui
C# Server-Side React

�������� �������
1. ������� ����� ������ Asp.net � ���������� Web Api 2
New Project -> Visual c# -> Windows -> Web -> Asp.net:
  Asp.net 4.5 template -> Empty project (Web Api: checked)
2. Install-Package NitroBolt.Wui

������� ������� � NitroBolt.Wui 1.x
1. ������ ����� View
  a. �����������
  b. ������������� � Controller � ������������� �� ApiController
  c. �������� ��� � HView � HttpContent �� HttpRequestMessage
  d. �������� Route
        [HttpGet, HttpPost]
        [Route(<��������>)]
        public HttpResponseMessage Route()
        {
            return HWebApiSynchronizeHandler.Process<MainState>(this.Request, HView);
        }