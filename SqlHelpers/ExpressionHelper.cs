using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DapperGenericRepository.SqlHelpers
{
    internal static class ExpressionHelper
    {
        public static string GetPropertyName<TSource, TField>(Expression<Func<TSource, TField>> field)
        {
            if (Equals(field, null))
            {
                throw new ArgumentNullException(nameof(field), "field can't be null");
            }

            MemberExpression expr = field.Body switch
            {
                MemberExpression body => body,
                UnaryExpression expression => (MemberExpression)expression.Operand,
                _ => throw new ArgumentException("Expression field isn't supported", nameof(field)),
            };
            return expr.Member.Name;
        }

        public static object GetValue(Expression member)
        {
            UnaryExpression objectMember = Expression.Convert(member, typeof(object));
            Expression<Func<object>> getterLambda = Expression.Lambda<Func<object>>(objectMember);
            Func<object> getter = getterLambda.Compile();
            return getter();
        }

        public static string GetSqlOperator(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.Equal or ExpressionType.Not or ExpressionType.MemberAccess => "=",
                ExpressionType.NotEqual => "!=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.AndAlso or ExpressionType.And => "AND",
                ExpressionType.Or or ExpressionType.OrElse => "OR",
                ExpressionType.Default => string.Empty,
                _ => throw new NotSupportedException(type + " isn't supported"),
            };
        }

        public static string GetSqlLikeValue(string methodName, object value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            return methodName switch
            {
                "StartsWith" => string.Format("{0}%", value),
                "EndsWith" => string.Format("%{0}", value),
                "StringContains" => string.Format("%{0}%", value),
                _ => throw new NotImplementedException(),
            };
        }

        public static string GetMethodCallSqlOperator(string methodName, bool isNotUnary = false)
        {
            return methodName switch
            {
                "StartsWith" or "EndsWith" or "StringContains" => isNotUnary ? "NOT LIKE" : "LIKE",
                "Contains" => isNotUnary ? "NOT IN" : "IN",
                "Any" or "All" => methodName.ToUpperInvariant(),
                _ => throw new NotSupportedException(methodName + " isn't supported"),
            };
        }

        public static BinaryExpression GetBinaryExpression(Expression expression)
        {
            BinaryExpression binaryExpression = expression as BinaryExpression;
            BinaryExpression body = binaryExpression ?? Expression.MakeBinary(ExpressionType.Equal, expression,
                expression.NodeType == ExpressionType.Not ? Expression.Constant(false) : Expression.Constant(true));
            return body;
        }

        public static Func<PropertyInfo, bool> GetPrimitivePropertiesPredicate()
        {
            return p => p.CanWrite && (p.PropertyType.IsValueType || p.PropertyType == typeof(string) || p.PropertyType == typeof(byte[]));
        }

        public static object GetValuesFromStringMethod(MethodCallExpression callExpr)
        {
            Expression expr = callExpr.Method.IsStatic ? callExpr.Arguments[1] : callExpr.Arguments[0];

            return GetValue(expr);
        }

        public static object GetValuesFromCollection(MethodCallExpression callExpr)
        {
            MemberExpression expr = (callExpr.Method.IsStatic ? callExpr.Arguments.First() : callExpr.Object)
                            as MemberExpression;

            if (expr?.Expression is not ConstantExpression)
            {
                throw new NotSupportedException(callExpr.Method.Name + " isn't supported");
            }

            ConstantExpression constExpr = (ConstantExpression)expr.Expression;

            Type constExprType = constExpr.Value.GetType();
            return constExprType.GetField(expr.Member.Name).GetValue(constExpr.Value);
        }

        public static MemberExpression GetMemberExpression(Expression expression)
        {
            switch (expression)
            {
                case MethodCallExpression expr:
                    return expr.Method.IsStatic
                        ? (MemberExpression)expr.Arguments.Last(x => x.NodeType == ExpressionType.MemberAccess)
                        : (MemberExpression)expr.Arguments[0];

                case MemberExpression memberExpression:
                    return memberExpression;

                case UnaryExpression unaryExpression:
                    return (MemberExpression)unaryExpression.Operand;

                case BinaryExpression binaryExpression:
                    BinaryExpression binaryExpr = binaryExpression;

                    if (binaryExpr.Left is UnaryExpression left)
                    {
                        return (MemberExpression)left.Operand;
                    }

                    //should we take care if right operation is memberaccess, not left?
                    return (MemberExpression)binaryExpr.Left;

                case LambdaExpression lambdaExpression:

                    switch (lambdaExpression.Body)
                    {
                        case MemberExpression body:
                            return body;

                        case UnaryExpression expressionBody:
                            return (MemberExpression)expressionBody.Operand;
                    }

                    break;
            }

            return null;
        }

        /// <summary>
        ///     Gets the name of the property.
        /// </summary>
        /// <param name="expr">The Expression.</param>
        /// <param name="nested">Out. Is nested property.</param>
        /// <returns>The property name for the property expression.</returns>
        public static string GetPropertyNamePath(Expression expr, out bool nested)
        {
            StringBuilder path = new();
            MemberExpression memberExpression = GetMemberExpression(expr);
            int count = 0;
            do
            {
                count++;
                if (path.Length > 0)
                {
                    _ = path.Insert(0, "");
                }

                _ = path.Insert(0, memberExpression.Member.Name);
                memberExpression = GetMemberExpression(memberExpression.Expression);
            } while (memberExpression != null);

            if (count > 2)
            {
                throw new ArgumentException("Only one degree of nesting is supported");
            }

            nested = count == 2;

            return path.ToString();
        }
    }
    internal class QueryBinaryExpression : QueryExpression
    {
        public QueryBinaryExpression()
        {
            NodeType = QueryExpressionType.Binary;
        }

        public List<QueryExpression> Nodes { get; set; }

        public override string ToString()
        {
            return $"[{base.ToString()} ({string.Join(",", Nodes)})]";
        }
    }
    internal abstract class QueryExpression
    {
        /// <summary>
        /// Query Expression Node Type
        /// </summary>
        public QueryExpressionType NodeType { get; set; }

        /// <summary>
        /// Operator OR/AND
        /// </summary>
        public string LinkingOperator { get; set; }

        public override string ToString()
        {
            return $"[NodeType:{NodeType}, LinkingOperator:{LinkingOperator}]";
        }
    }
    internal enum QueryExpressionType
    {
        Parameter = 0,
        Binary = 1,
    }
    internal class QueryParameterExpression : QueryExpression
    {
        public QueryParameterExpression()
        {
            NodeType = QueryExpressionType.Parameter;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="QueryParameterExpression " /> class.
        /// </summary>
        /// <param name="linkingOperator">The linking operator.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="queryOperator">The query operator.</param>
        /// <param name="nestedProperty">Signilize if it is nested property.</param>
        internal QueryParameterExpression(string linkingOperator,
            string propertyName, object propertyValue,
            string queryOperator, bool nestedProperty) : this()
        {
            LinkingOperator = linkingOperator;
            PropertyName = propertyName;
            PropertyValue = propertyValue;
            QueryOperator = queryOperator;
            NestedProperty = nestedProperty;
        }

        public string PropertyName { get; set; }
        public object PropertyValue { get; set; }
        public string QueryOperator { get; set; }
        public bool NestedProperty { get; set; }

        public override string ToString()
        {
            return
                $"[{base.ToString()}, PropertyName:{PropertyName}, PropertyValue:{PropertyValue}, QueryOperator:{QueryOperator}, NestedProperty:{NestedProperty}]";
        }
    }
}
