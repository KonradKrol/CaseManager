using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using CaseManager.Dto;
using CaseManager.Exceptions;
using CaseManager.Models;
using CaseManager.Repository;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CaseManager.Controllers;

[ApiController]
[Route("/comments")]
public class CommentsController(
    IMapper mapper,
    IValidator<AddCommentDto> addCommentDtoValidator,
    ICommentRepository commentRepository,
    ICaseRepository caseRepository) : ControllerBase
{
    [HttpGet]
    [Route(("/comments/{id:guid}"))]
    public async Task<IActionResult> GetComment([FromRoute] Guid id, CommentDetailsDtoValidator outputValidator)
    {
        var comment = await commentRepository.GetCommentById(id);

        if (comment is null)
        {
            return NotFound();
        }

        var commentDetailsDto = mapper.Map<CommentDetailsDto>(comment);

        outputValidator.ValidateOutputDtoAndThrow(commentDetailsDto);

        return Ok(commentDetailsDto);
    }

    [HttpGet]
    [Route("/comments")]
    public async Task<IActionResult> GetComments([FromQuery] [Required] Guid caseId,
        CommentDetailsDtoValidator outputValidator)
    {
        var caseExists = await caseRepository.CaseExists(caseId);
        if (!caseExists) throw new CaseNotExistsException(caseId);

        var comments = (await commentRepository.GetAllCommentsByCaseId(caseId)).ToImmutableList();

        if (comments.Count == 0) return NoContent();

        var commentDetailsDtos = comments.Select(mapper.Map<CommentDetailsDto>).ToImmutableList();

        outputValidator.ValidateOutputDtosAndThrowFirstError(commentDetailsDtos);

        return Ok(commentDetailsDtos);
    }

    [HttpPost]
    public async Task<IActionResult> AddComment([FromBody] AddCommentDto addCommentDto)
    {
        await addCommentDtoValidator.ValidateAndThrowAsync(addCommentDto);

        var caseExists = await caseRepository.CaseExists(addCommentDto.CaseId);
        if (!caseExists)
        {
            throw new CaseNotExistsException(addCommentDto.CaseId);
        }

        var commentId = Guid.NewGuid();

        var comment = mapper.Map<Comment>(addCommentDto, opt => { opt.Items["Id"] = commentId; });
        var createdAt = DateTime.Now;

        await commentRepository.AddComment(comment);

        var commentAdded = new CommentAddedDto()
        {
            CommentId = commentId,
            CreatedAt = createdAt,
        };

        return Ok(commentAdded);
    }
}