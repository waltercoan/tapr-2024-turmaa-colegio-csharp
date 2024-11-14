using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using microservcolegio.Secretaria.Entities;
using microservcolegio.Secretaria.Entities.Services;
using Microsoft.AspNetCore.Mvc;

namespace microservcolegio.Secretaria.Controllers;

[ApiController]
[Route("/api/v1/[controller]")]
public class AlunoController : ControllerBase
{
    private IAlunoService _service;
    public AlunoController(IAlunoService service){
        this._service = service;
    }
    [HttpGet]
    public async Task<IResult> Get(){
        var listaAlunos = await _service.GetAllAsync();
        return Results.Ok(listaAlunos);
    }

    [HttpPost]
    public async Task<IResult> Post(Aluno aluno){
        if(aluno == null)
        {
            return Results.BadRequest();
        }
        var alunoSalvo = await _service.SaveAsync(aluno);
        return Results.Ok(alunoSalvo);
    }
    [HttpPut("{id}")]
    public async Task<IResult> Put(string id, [FromBody] Aluno aluno)
    {
        if(aluno == null || id.Equals(String.Empty))
        {
            return Results.BadRequest();
        }
        aluno = await _service.UpdateAsync(id,aluno);
        if(aluno == null)
        {
            return Results.NotFound();
        }
        return Results.Ok(aluno);
    }
    [HttpDelete("{id}")]
    public async Task<IResult> Delete(string id)
    {
        if(id.Equals(String.Empty))
        {
            return Results.BadRequest();
        }
        var aluno = await this._service.DeleteAsync(id);
         if(aluno == null)
        {
            return Results.NotFound();
        }
        return Results.Ok(aluno);
    }
    
}
