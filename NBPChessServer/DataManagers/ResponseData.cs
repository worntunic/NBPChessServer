using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using RedisData;

namespace NBPChessServer.DataManagers
{
    public class ResponseData
    {
        private Object generalData;
        public Object data;
        public int code;
        public string message;
        protected Dictionary<string, object> generalForm = new Dictionary<string, object>();
        protected const string codeKey = "code", messageKey = "message", dataKey = "data";

        public ResponseData(int code, string message, Object data = null)
        {
            this.code = code;
            this.message = message;
            this.data = data;
        }

        public ActionResult GetActionResult()
        {
            AssignGeneralData();
            if (code == 200)
            {
                return new OkObjectResult(generalData);
            }
            else
            {
                return new BadRequestObjectResult(generalData);
            }
        }

        protected virtual void PrepareData()
        {
            generalForm.Add(dataKey, data);
        }
        private void AssignGeneralData()
        {
            generalForm.Add(codeKey, code);
            generalForm.Add(messageKey, message);

            PrepareData();
            generalData = generalForm;
        }
    }

}

