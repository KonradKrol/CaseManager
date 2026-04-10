using AutoMapper;
using CaseManager.Dto;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CaseManager.Controllers;

[ApiController]
[Route("/comments")]
public class CommentsController(IMapper mapper, IValidator<AddCommentDto> addCommentDtoValidator) : ControllerBase
{
    [HttpGet]
    [Route(("/comments/{id:guid}"))]
    public IActionResult GetComment([FromRoute] Guid id)
    {
        var commentDetailsDto = new Dictionary<string, string>() { };

        return Ok(commentDetailsDto);
    }

    [HttpPost]
    public IActionResult AddComment([FromBody] AddCommentDto addCommentDto)
    {
        addCommentDtoValidator.ValidateAndThrow(addCommentDto);

        var commentAdded = new CommentAddedDto()
        {
            CommentId = Guid.NewGuid(),
            CreatedAt = DateTime.Now,
        };

        // TODO
        return Ok(commentAdded);
    }
}