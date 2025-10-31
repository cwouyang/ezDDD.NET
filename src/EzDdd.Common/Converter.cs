namespace EzDdd.Common;

/// <summary>
///     Delegate for type conversion between layers.
///     This delegate provides a standard contract for converting values from one type to another,
///     commonly used in Domain-Driven Design for mapping between entities, DTOs, and data models.
///     Equivalent to Java's <c>@FunctionalInterface</c> annotation.
/// </summary>
/// <typeparam name="TSource">The source type to convert from</typeparam>
/// <typeparam name="TTarget">The target type to convert to</typeparam>
/// <param name="source">The source value to convert</param>
/// <returns>The converted target value</returns>
/// <remarks>
///     <para>
///         This delegate supports both lambda expressions and method references,
///         providing the same semantics as Java's functional interface.
///         The use of 'in' (contravariance) and 'out' (covariance) modifiers enables flexible
///         type relationships in conversion scenarios.
///     </para>
///     <para>
///         <b>Examples:</b>
///         <code>
/// // Lambda implementation
/// Converter&lt;string, int&gt; stringToInt = s =&gt; int.Parse(s);
/// int result = stringToInt("42");
/// 
/// // Method reference
/// Converter&lt;string, int&gt; parser = int.Parse;
/// 
/// // Multi-line lambda
/// Converter&lt;User, UserDto&gt; converter = user =&gt;
/// {
///     return new UserDto(user.Id, user.Name);
/// };
/// </code>
///     </para>
///     <para>
///         <b>Null Handling:</b>
///         Implementations should handle null values appropriately based on their conversion logic.
///         Consider using nullable reference types in the generic parameters when null values are expected.
///     </para>
/// </remarks>
public delegate TTarget Converter<in TSource, out TTarget>(TSource source);