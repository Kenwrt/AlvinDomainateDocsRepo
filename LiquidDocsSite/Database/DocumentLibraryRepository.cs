using LiquidDocsData.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiquidDocsSite.Database;

public interface IDocumentLibraryRepository : IBaseRepository<DocumentLibrary>
{
    Task<IEnumerable<DocumentLibrary>> GetForLoanAsync(Guid loanApplicationId);
}

public class DocumentLibraryRepository : BaseRepository<DocumentLibrary>, IDocumentLibraryRepository
{
    public DocumentLibraryRepository(IMongoDatabaseRepo db) : base(db)
    {
    }

    public async Task<IEnumerable<DocumentLibrary>> GetForLoanAsync(Guid loanApplicationId)
    {
        var all = await Db.GetRecordsAsync<DocumentLibrary>();
        return all.Where(x => x.LoanApplicationId == loanApplicationId);
    }
}
