using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentProcessing.Domain.Models;

namespace PaymentProcessing.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for StateTransition entity
/// </summary>
public sealed class StateTransitionConfiguration : IEntityTypeConfiguration<StateTransition>
{
    public void Configure(EntityTypeBuilder<StateTransition> builder)
    {
        builder.ToTable("state_transitions");

        builder.HasKey(st => st.Id);
        builder.HasIndex(st => st.PaymentId);

        builder.Property(st => st.PaymentId)
            .IsRequired();

        builder.Property(st => st.FromStatus)
            .IsRequired();

        builder.Property(st => st.ToStatus)
            .IsRequired();

        builder.Property(st => st.TransitionedAt)
            .IsRequired();
    }
}
