﻿using System;
using System.Linq.Expressions;

namespace Sql2Sql.ExprRewrite
{
    /// <summary>
    /// Una excepción del RuleRewriter
    /// </summary>
    public class ApplyRuleException : Exception
    {
        public ApplyRuleException(string message, string rule,Expression expr, Exception innerException) 
            : base($"rule: '{rule}', message: '{message}', expr: '{expr}'", innerException)
        {
        }
    }
}
