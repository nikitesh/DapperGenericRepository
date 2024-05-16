using System.Linq.Expressions;
using System.Text;

namespace DapperGenericRepository.SqlHelpers
{
    public static class QueryHelper
    {
        public static string Where<T>(StringBuilder sqlBuilder, Expression<Func<T, bool>> predicate, ref IDictionary<string, object> dictionaryParams)
            where T : class
        {
            List<QueryExpression> queryProperties = GetQueryProperties(predicate.Body);
            int qLevel = 0;
            List<KeyValuePair<string, object>> conditions = new();
            BuildQuerySql(queryProperties, ref sqlBuilder, ref conditions, ref qLevel);
            dictionaryParams.AddRange(conditions);
            return sqlBuilder.ToString().Trim();

        }
        private static void BuildQuerySql(IList<QueryExpression> queryProperties,
              ref StringBuilder sqlBuilder, ref List<KeyValuePair<string, object>> conditions, ref int qLevel)
        {
            foreach (QueryExpression expr in queryProperties)
            {
                if (!string.IsNullOrEmpty(expr.LinkingOperator))
                {
                    if (sqlBuilder.Length > 0)
                    {
                        _ = sqlBuilder.Append(' ');
                    }

                    _ = sqlBuilder
                        .Append(expr.LinkingOperator)
                        .Append(' ');
                }

                switch (expr)
                {
                    case QueryParameterExpression qpExpr:
                        if (qpExpr.PropertyValue == null)
                        {
                            _ = sqlBuilder.AppendFormat("{0} {1} NULL", qpExpr.PropertyName, qpExpr.QueryOperator == "=" ? "IS" : "IS NOT");
                        }
                        else
                        {
                            string vKey = string.Format("{0}_p{1}", qpExpr.PropertyName, qLevel); //Handle multiple uses of a field

                            _ = sqlBuilder.AppendFormat("{0} {1} @{2}", qpExpr.PropertyName, qpExpr.QueryOperator, vKey);
                            conditions.Add(new KeyValuePair<string, object>(vKey, qpExpr.PropertyValue));
                        }

                        qLevel++;
                        break;

                    case QueryBinaryExpression qbExpr:
                        StringBuilder nSqlBuilder = new();
                        List<KeyValuePair<string, object>> nConditions = new();
                        BuildQuerySql(qbExpr.Nodes, ref nSqlBuilder, ref nConditions, ref qLevel);

                        _ = qbExpr.Nodes.Count == 1 ? sqlBuilder.Append(nSqlBuilder) : sqlBuilder.AppendFormat("({0})", nSqlBuilder);

                        conditions.AddRange(nConditions);
                        break;
                }
            }
        }
        private static List<QueryExpression> GetQueryProperties(Expression expr)
        {
            QueryExpression queryNode = GetQueryProperties(expr, ExpressionType.Default);
            return queryNode switch
            {
                QueryParameterExpression => new List<QueryExpression> { queryNode },
                QueryBinaryExpression qbExpr => qbExpr.Nodes,
                _ => throw new NotSupportedException(queryNode.ToString()),
            };
        }
        private static QueryExpression GetQueryProperties(Expression expr, ExpressionType linkingType)
        {
            bool isNotUnary = false;
            if (expr is UnaryExpression unaryExpression)
            {
                if (unaryExpression.NodeType == ExpressionType.Not && unaryExpression.Operand is MethodCallExpression)
                {
                    expr = unaryExpression.Operand;
                    isNotUnary = true;
                }
            }

            if (expr is MethodCallExpression methodCallExpression)
            {
                string methodName = methodCallExpression.Method.Name;
                Expression exprObj = methodCallExpression.Object;
            MethodLabel:
                switch (methodName)
                {
                    case "Contains":
                    case "Exists":
                        {
                            if (exprObj != null
                                && exprObj.NodeType == ExpressionType.MemberAccess
                                && exprObj.Type == typeof(string))
                            {
                                methodName = "StringContains";
                                goto MethodLabel;
                            }

                            string propertyName = ExpressionHelper.GetPropertyNamePath(methodCallExpression, out bool isNested);

                            //if (!SqlProperties.Select(x => x.PropertyName).Contains(propertyName) &&
                            //    !SqlJoinProperties.Select(x => x.PropertyName).Contains(propertyName))
                            //    throw new NotSupportedException("predicate can't parse");

                            object propertyValue = ExpressionHelper.GetValuesFromCollection(methodCallExpression);
                            string opr = ExpressionHelper.GetMethodCallSqlOperator(methodName, isNotUnary);
                            string link = ExpressionHelper.GetSqlOperator(linkingType);
                            return new QueryParameterExpression(link, propertyName, propertyValue, opr, isNested);
                        }
                    case "StringContains":
                    case "StartsWith":
                    case "EndsWith":
                        {
                            if (exprObj == null
                                || exprObj.NodeType != ExpressionType.MemberAccess
                                || exprObj.Type != typeof(string))
                            {
                                goto default;
                            }

                            string propertyName = ExpressionHelper.GetPropertyNamePath(exprObj, out bool isNested);

                            //if (!SqlProperties.Select(x => x.PropertyName).Contains(propertyName) &&
                            //    !SqlJoinProperties.Select(x => x.PropertyName).Contains(propertyName))
                            //    throw new NotSupportedException("predicate can't parse");

                            object propertyValue = ExpressionHelper.GetValuesFromStringMethod(methodCallExpression);
                            string likeValue = ExpressionHelper.GetSqlLikeValue(methodName, propertyValue);
                            string opr = ExpressionHelper.GetMethodCallSqlOperator(methodName, isNotUnary);
                            string link = ExpressionHelper.GetSqlOperator(linkingType);
                            return new QueryParameterExpression(link, propertyName, likeValue, opr, isNested);
                        }
                    default:
                        throw new NotSupportedException($"'{methodName}' method is not supported");
                }
            }

            if (expr is BinaryExpression binaryExpression)
            {
                if (binaryExpression.NodeType is not ExpressionType.AndAlso and not ExpressionType.OrElse)
                {
                    string propertyName = ExpressionHelper.GetPropertyNamePath(binaryExpression, out bool isNested);

                    //if (!SqlProperties.Select(x => x.PropertyName).Contains(propertyName) &&
                    //    !SqlJoinProperties.Select(x => x.PropertyName).Contains(propertyName))
                    //    throw new NotSupportedException("predicate can't parse");

                    object propertyValue = ExpressionHelper.GetValue(binaryExpression.Right);
                    string opr = ExpressionHelper.GetSqlOperator(binaryExpression.NodeType);
                    string link = ExpressionHelper.GetSqlOperator(linkingType);

                    return new QueryParameterExpression(link, propertyName, propertyValue, opr, isNested);
                }

                QueryExpression leftExpr = GetQueryProperties(binaryExpression.Left, ExpressionType.Default);
                QueryExpression rightExpr = GetQueryProperties(binaryExpression.Right, binaryExpression.NodeType);

                switch (leftExpr)
                {
                    case QueryParameterExpression lQPExpr:
                        if (!string.IsNullOrEmpty(lQPExpr.LinkingOperator) && !string.IsNullOrEmpty(rightExpr.LinkingOperator)) // AND a AND B
                        {
                            switch (rightExpr)
                            {
                                case QueryBinaryExpression rQBExpr:
                                    if (lQPExpr.LinkingOperator == rQBExpr.Nodes.Last().LinkingOperator) // AND a AND (c AND d)
                                    {
                                        QueryBinaryExpression nodes = new()
                                        {
                                            LinkingOperator = leftExpr.LinkingOperator,
                                            Nodes = new List<QueryExpression> { leftExpr }
                                        };

                                        rQBExpr.Nodes[0].LinkingOperator = rQBExpr.LinkingOperator;
                                        nodes.Nodes.AddRange(rQBExpr.Nodes);

                                        leftExpr = nodes;
                                        rightExpr = null;
                                        // AND a AND (c AND d) => (AND a AND c AND d)
                                    }
                                    break;
                            }
                        }
                        break;

                    case QueryBinaryExpression lQBExpr:
                        switch (rightExpr)
                        {
                            case QueryParameterExpression rQPExpr:
                                if (rQPExpr.LinkingOperator == lQBExpr.Nodes.Last().LinkingOperator)    //(a AND b) AND c
                                {
                                    lQBExpr.Nodes.Add(rQPExpr);
                                    rightExpr = null;
                                    //(a AND b) AND c => (a AND b AND c)
                                }
                                break;

                            case QueryBinaryExpression rQBExpr:
                                if (lQBExpr.Nodes.Last().LinkingOperator == rQBExpr.LinkingOperator) // (a AND b) AND (c AND d)
                                {
                                    if (rQBExpr.LinkingOperator == rQBExpr.Nodes.Last().LinkingOperator)   // AND (c AND d)
                                    {
                                        rQBExpr.Nodes[0].LinkingOperator = rQBExpr.LinkingOperator;
                                        lQBExpr.Nodes.AddRange(rQBExpr.Nodes);
                                        // (a AND b) AND (c AND d) =>  (a AND b AND c AND d)
                                    }
                                    else
                                    {
                                        lQBExpr.Nodes.Add(rQBExpr);
                                        // (a AND b) AND (c OR d) =>  (a AND b AND (c OR d))
                                    }
                                    rightExpr = null;
                                }
                                break;
                        }
                        break;
                }

                string nLinkingOperator = ExpressionHelper.GetSqlOperator(linkingType);
                if (rightExpr == null)
                {
                    leftExpr.LinkingOperator = nLinkingOperator;
                    return leftExpr;
                }

                return new QueryBinaryExpression
                {
                    NodeType = QueryExpressionType.Binary,
                    LinkingOperator = nLinkingOperator,
                    Nodes = new List<QueryExpression> { leftExpr, rightExpr },
                };
            }

            return GetQueryProperties(ExpressionHelper.GetBinaryExpression(expr), linkingType);
        }
        public static string GetNames<T>(Expression<Func<T, object>> predicate) where T : class
        {
            List<string> propertyList = new();
            List<QueryExpression> queryProperties = GetQueryProperties(predicate.Body);
            foreach (QueryParameterExpression expr in queryProperties)
            {
                propertyList.Add(expr.PropertyName);
            }
            return string.Join(',', propertyList);
        }
    }
    public static class CommonExtensions
    {

        public static void AddRange<TInput>(this ICollection<TInput> collection, IEnumerable<TInput> addCollection)
        {
            if (collection == null || addCollection == null)
            {
                return;
            }

            foreach (TInput item in addCollection)
            {
                collection.Add(item);
            }
        }
    }
}
