﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuParsers.Helpers
{
    public class FormatHelper
    {
        public static string Join(IEnumerable<string> vs, string splitter = " ")
        {
            string owo = string.Empty;
            vs.ToList().ForEach(e => owo += e + splitter);
            return owo;
        }
    }
}
