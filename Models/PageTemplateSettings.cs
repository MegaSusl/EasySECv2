using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Models
{
    public class PageTemplateSettings
    {
        public string PageKey { get; set; } = string.Empty;
        public bool AllowBatch { get; set; } = false;
        // В будущем сюда можно добавить предустановленные таблицы и т.п.
    }
}

