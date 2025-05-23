using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasySECv2.Models;

namespace EasySECv2.Services
{
    public interface IDefaultMappingService
    {
        /// <summary>
        /// Возвращает дефолтную настройку для маркера, либо null.
        /// </summary>
        DefaultMapping? Get(string placeholder);
    }
}
