namespace DapperGenericRepository.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DapperKey : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DapperIgnore : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DapperIncludeInWhere : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DapperExcludeInInsert : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DapperExcludeInUpdate : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DapperClassName : Attribute
    {
        public string ClassName { get; set; }
        public DapperClassName(string className)
        {
            ClassName = className;
        }
    }
}
