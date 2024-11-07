using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace microservcolegio.Secretaria.Entities.Services;

public interface IAlunoService
{
    Task<List<Aluno>> GetAllAsync();
    Task<Aluno> SaveAsync(Aluno aluno);
}
