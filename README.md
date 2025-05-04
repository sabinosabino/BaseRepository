# Sabino.BaseRepository

Uma implementação leve de repositório base utilizando **Dapper** para simplificar operações CRUD em aplicações .NET.

[![NuGet](https://www.nuget.org/packages/Sabino.BaseRepository/)]

## ✨ Recursos

- Operações CRUD básicas prontas para uso
- Alta performance com Dapper
- Design simples e extensível
- Reduz a repetição de código na camada de acesso a dados

---

## 📦 Instalação

Você pode instalar via **NuGet Package Manager**:

```bash
Install-Package Sabino.BaseRepository
```

---

## 🛠️ Como usar

### 1. Defina seu modelo com atributos de mapeamento:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("clientes")]
public class Clientes
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public DateTime DataNascimento { get; set; }

    [Required]
    public decimal? Salario { get; set; }

    public int? Quantidade { get; set; }
}
```

### 2. Instancie o `DbContext` com sua conexão:

```csharp
using MySql.Data.MySqlClient;

var connectionString = "Server=localhost;Port=3306;Database=teste;Uid=root;Pwd=123456;";
var db = new DbContext(new MySqlConnection(connectionString));
```

### 3. Faça uma inserção no banco:

```csharp
var novoCliente = new Clientes
{
    Nome = Guid.NewGuid().ToString(),
    DataNascimento = DateTime.Now,
    Salario = 10002
};

await db.InsertAsync(novoCliente);
```

---

## 🧱 Criando seu próprio repositório

Você pode criar repositórios específicos herdando de `RepositoryBase<T>`:

```csharp
public class ClientesRepository : RepositoryBase<Clientes>
{
    private readonly DbContext _dbContext;

    public ClientesRepository(DbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }
}
```

---

## 📷 Exemplo de uso na prática

> Exemplo visual do fluxo:

![Exemplo de uso](https://github.com/user-attachments/assets/cbfd9a9b-c669-4309-ab8f-09b89fb8b47f)

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para abrir *issues* e *pull requests*.

---

## 📄 Licença

Este projeto está licenciado sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## 📬 Contato

Em caso de dúvidas, sugestões ou melhorias, abra uma issue ou entre em contato diretamente --> sabinowelbert@gmail.com.
