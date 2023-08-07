using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace HotChocolate.OpenApi.Tests.Controller;

/// <summary>
///
/// </summary>
[DataContract]
public class NewPet : IEquatable<NewPet>
{
    /// <summary>
    /// Gets or Sets Name
    /// </summary>
    [Required]
    [DataMember(Name = "name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or Sets Tag
    /// </summary>

    [DataMember(Name = "tag")]
    public string Tag { get; set; }

    /// <summary>
    /// Returns true if objects are equal
    /// </summary>
    /// <param name="obj">Object to be compared</param>
    /// <returns>Boolean</returns>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((NewPet)obj);
    }

    /// <summary>
    /// Returns true if NewPet instances are equal
    /// </summary>
    /// <param name="other">Instance of NewPet to be compared</param>
    /// <returns>Boolean</returns>
    public bool Equals(NewPet other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return
            (
                Name == other.Name ||
                Name != null &&
                Name.Equals(other.Name)
            ) &&
            (
                Tag == other.Tag ||
                Tag != null &&
                Tag.Equals(other.Tag)
            );
    }

    /// <summary>
    /// Gets the hash code
    /// </summary>
    /// <returns>Hash code</returns>
    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            var hashCode = 41;
            // Suitable nullity checks etc, of course :)
            if (Name != null)
                hashCode = hashCode * 59 + Name.GetHashCode();
            if (Tag != null)
                hashCode = hashCode * 59 + Tag.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Returns the JSON string presentation of the object
    /// </summary>
    /// <returns>JSON string presentation of the object</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    /// <summary>
    /// Returns the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("class NewPet {\n");
        sb.Append("  Name: ").Append(Name).Append("\n");
        sb.Append("  Tag: ").Append(Tag).Append("\n");
        sb.Append("}\n");
        return sb.ToString();
    }

    #region Operators

#pragma warning disable 1591

    public static bool operator ==(NewPet left, NewPet right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(NewPet left, NewPet right)
    {
        return !Equals(left, right);
    }

#pragma warning restore 1591

    #endregion Operators
}

/// <summary>
///
/// </summary>
[DataContract]
public class Pet : NewPet, IEquatable<Pet>
{
    /// <summary>
    /// Gets or Sets Id
    /// </summary>
    [Required]
    [DataMember(Name = "id")]
    public long? Id { get; set; }

    /// <summary>
    /// Returns true if objects are equal
    /// </summary>
    /// <param name="obj">Object to be compared</param>
    /// <returns>Boolean</returns>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Pet)obj);
    }

    /// <summary>
    /// Returns true if Pet instances are equal
    /// </summary>
    /// <param name="other">Instance of Pet to be compared</param>
    /// <returns>Boolean</returns>
    public bool Equals(Pet other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return
        (
            Id == other.Id ||
            Id != null &&
            Id.Equals(other.Id)
        );
    }

    /// <summary>
    /// Gets the hash code
    /// </summary>
    /// <returns>Hash code</returns>
    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            var hashCode = 41;
            // Suitable nullity checks etc, of course :)
            if (Id != null)
                hashCode = hashCode * 59 + Id.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Returns the JSON string presentation of the object
    /// </summary>
    /// <returns>JSON string presentation of the object</returns>
    public new string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    /// <summary>
    /// Returns the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("class Pet {\n");
        sb.Append("  Id: ").Append(Id).Append("\n");
        sb.Append("}\n");
        return sb.ToString();
    }

    #region Operators

#pragma warning disable 1591

    public static bool operator ==(Pet left, Pet right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Pet left, Pet right)
    {
        return !Equals(left, right);
    }

#pragma warning restore 1591

    #endregion Operators
}

/// <summary>
///
/// </summary>
[DataContract]
public class Error : IEquatable<Error>
{
    /// <summary>
    /// Gets or Sets Code
    /// </summary>
    [Required]
    [DataMember(Name = "code")]
    public int? Code { get; set; }

    /// <summary>
    /// Gets or Sets Message
    /// </summary>
    [Required]
    [DataMember(Name = "message")]
    public string Message { get; set; }

    /// <summary>
    /// Returns true if objects are equal
    /// </summary>
    /// <param name="obj">Object to be compared</param>
    /// <returns>Boolean</returns>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Error)obj);
    }

    /// <summary>
    /// Returns true if Error instances are equal
    /// </summary>
    /// <param name="other">Instance of Error to be compared</param>
    /// <returns>Boolean</returns>
    public bool Equals(Error other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return
            (
                Code == other.Code ||
                Code != null &&
                Code.Equals(other.Code)
            ) &&
            (
                Message == other.Message ||
                Message != null &&
                Message.Equals(other.Message)
            );
    }

    /// <summary>
    /// Gets the hash code
    /// </summary>
    /// <returns>Hash code</returns>
    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            var hashCode = 41;
            // Suitable nullity checks etc, of course :)
            if (Code != null)
                hashCode = hashCode * 59 + Code.GetHashCode();
            if (Message != null)
                hashCode = hashCode * 59 + Message.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Returns the JSON string presentation of the object
    /// </summary>
    /// <returns>JSON string presentation of the object</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    /// <summary>
    /// Returns the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("class Error {\n");
        sb.Append("  Code: ").Append(Code).Append("\n");
        sb.Append("  Message: ").Append(Message).Append("\n");
        sb.Append("}\n");
        return sb.ToString();
    }

    #region Operators

#pragma warning disable 1591

    public static bool operator ==(Error left, Error right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Error left, Error right)
    {
        return !Equals(left, right);
    }

#pragma warning restore 1591

    #endregion Operators
}

/// <summary>
///
/// </summary>
[ApiController]
public class DefaultApiController : ControllerBase
{
    private static List<Pet> _pets = new()
    {
        new Pet
        {
            Id = 1,
            Name = "Chooper"
        },
        new Pet
        {
            Id = 2,
            Name = "Rex"
        }
    };

    /// <summary>
    ///
    /// </summary>
    /// <remarks>Creates a new pet in the store. Duplicates are allowed</remarks>
    /// <param name="body">Pet to add to the store</param>
    /// <response code="200">pet response</response>
    /// <response code="0">unexpected error</response>
    [HttpPost]
    [Route("/pets")]
    [ValidateModelState]
    [SwaggerOperation("AddPet")]
    [SwaggerResponse(200, type: typeof(Pet), description: "pet response")]
    [SwaggerResponse(0, type: typeof(Error), description: "unexpected error")]
    public virtual IActionResult AddPet([FromBody] NewPet body)
    {
        var newPet = new Pet
        {
            Id = _pets.Max(p => p.Id) + 1,
            Name = body.Name,
            Tag = body.Tag
        };

        _pets.Add(newPet);
        return new ObjectResult(newPet);
    }

    /// <summary>
    ///
    /// </summary>
    /// <remarks>deletes a single pet based on the ID supplied</remarks>
    /// <param name="id">ID of pet to delete</param>
    /// <response code="204">pet deleted</response>
    /// <response code="0">unexpected error</response>
    [HttpDelete]
    [Route("/pets/{id}")]
    [ValidateModelState]
    [SwaggerOperation("DeletePet")]
    [SwaggerResponse(0, type: typeof(Error), description: "unexpected error")]
    public virtual IActionResult DeletePet([FromRoute] [Required] long? id)
    {
        var toDelete = _pets.FirstOrDefault(p => p.Id == id);

        if (toDelete is not null)
        {
            _pets.Remove(toDelete);
            return Ok();
        }

        return BadRequest("Pet not found");
    }

    /// <summary>
    ///
    /// </summary>
    /// <remarks>Returns a user based on a single ID, if the user does not have access to the pet</remarks>
    /// <param name="id">ID of pet to fetch</param>
    /// <response code="200">pet response</response>
    /// <response code="0">unexpected error</response>
    [HttpGet]
    [Route("/pets/{id}")]
    [ValidateModelState]
    [SwaggerOperation("FindPetById")]
    [SwaggerResponse(200, type: typeof(Pet), description: "pet response")]
    [SwaggerResponse(0, type: typeof(Error), description: "unexpected error")]
    public virtual IActionResult FindPetById([FromRoute] [Required] long? id)
    {
        return new ObjectResult(_pets.FirstOrDefault(p => p.Id == id));
    }

    /// <summary>
    ///
    /// </summary>
    /// <remarks>Returns all pets from the system that the user has access to Nam sed condimentum est. Maecenas tempor sagittis sapien, nec rhoncus sem sagittis sit amet. Aenean at gravida augue, ac iaculis sem. Curabitur odio lorem, ornare eget elementum nec, cursus id lectus. Duis mi turpis, pulvinar ac eros ac, tincidunt varius justo. In hac habitasse platea dictumst. Integer at adipiscing ante, a sagittis ligula. Aenean pharetra tempor ante molestie imperdiet. Vivamus id aliquam diam. Cras quis velit non tortor eleifend sagittis. Praesent at enim pharetra urna volutpat venenatis eget eget mauris. In eleifend fermentum facilisis. Praesent enim enim, gravida ac sodales sed, placerat id erat. Suspendisse lacus dolor, consectetur non augue vel, vehicula interdum libero. Morbi euismod sagittis libero sed lacinia.  Sed tempus felis lobortis leo pulvinar rutrum. Nam mattis velit nisl, eu condimentum ligula luctus nec. Phasellus semper velit eget aliquet faucibus. In a mattis elit. Phasellus vel urna viverra, condimentum lorem id, rhoncus nibh. Ut pellentesque posuere elementum. Sed a varius odio. Morbi rhoncus ligula libero, vel eleifend nunc tristique vitae. Fusce et sem dui. Aenean nec scelerisque tortor. Fusce malesuada accumsan magna vel tempus. Quisque mollis felis eu dolor tristique, sit amet auctor felis gravida. Sed libero lorem, molestie sed nisl in, accumsan tempor nisi. Fusce sollicitudin massa ut lacinia mattis. Sed vel eleifend lorem. Pellentesque vitae felis pretium, pulvinar elit eu, euismod sapien. </remarks>
    /// <param name="tags">tags to filter by</param>
    /// <param name="limit">maximum number of results to return</param>
    /// <response code="200">pet response</response>
    /// <response code="0">unexpected error</response>
    [HttpGet]
    [Route("/pets")]
    [ValidateModelState]
    [SwaggerOperation("FindPets")]
    [SwaggerResponse(200, type: typeof(List<Pet>), description: "pet response")]
    [SwaggerResponse(0, type: typeof(Error), description: "unexpected error")]
    public virtual IActionResult FindPets([FromQuery] List<string> tags, [FromQuery] int? limit)
    {
        return new ObjectResult(_pets);
    }
}

/// <summary>
/// Model state validation attribute
/// </summary>
public class ValidateModelStateAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Called before the action method is invoked
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Per https://blog.markvincze.com/how-to-validate-action-parameters-with-dataannotation-attributes/
        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        if (descriptor != null)
            foreach (var parameter in descriptor.MethodInfo.GetParameters())
            {
                object args = null;
                if (context.ActionArguments.ContainsKey(parameter.Name)) args = context.ActionArguments[parameter.Name];

                ValidateAttributes(parameter, args, context.ModelState);
            }

        if (!context.ModelState.IsValid) context.Result = new BadRequestObjectResult(context.ModelState);
    }

    private void ValidateAttributes(ParameterInfo parameter, object args, ModelStateDictionary modelState)
    {
        foreach (var attributeData in parameter.CustomAttributes)
        {
            var attributeInstance = parameter.GetCustomAttribute(attributeData.AttributeType);

            var validationAttribute = attributeInstance as ValidationAttribute;
            if (validationAttribute != null)
            {
                var isValid = validationAttribute.IsValid(args);
                if (!isValid)
                    modelState.AddModelError(parameter.Name, validationAttribute.FormatErrorMessage(parameter.Name));
            }
        }
    }
}
