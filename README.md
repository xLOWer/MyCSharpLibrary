# DbManager v0.1a 

## How to use

* Install NuGet package Newtonsoft.Json
* Create class
* Mark it with attribute (else class name is considered the name of the table)
```CSharp 
[TableManagerAttribute(SchemeName="name", TableName="name")] 
``` 
* Configure connection of type you want
```CSharp
Connection.Configure();
``` 

## Example

```CSharp
namespace MyNamespace
{
  [TableManagerAttribute(TableName="test")] 
  public class Test
  {  
    public int Id { get; set; }  
    public string Name { get; set; }  
  }

  public void Main()
  {
    // configure new connection
    DbManager.Connection.Configure( new MySqlConnection( connString ) );   
    
    // insert new row from class
    DbManager.InsertNewEntity<Test,MySqlCommand>(  
      new TestModel() 
      { 
        Id=0, 
        Name="name0" 
      }
    );
    // translate to sql string "INSERT INTO test (Id, Name) VALUES (0,`name0`)"
    
    // select all rows from table
    var listOfTestEntities = DbManager.SelectEntity<Test, MySqlCommand>();
    // translate to sql string "SELECT * FROM test"
  }
}
```

If you find bugs/errors or want to suggest something, then write to the mail: `venick007@gmail.com`
