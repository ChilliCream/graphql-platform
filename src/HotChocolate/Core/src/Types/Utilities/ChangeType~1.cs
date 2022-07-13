namespace HotChocolate.Utilities;

public delegate TTo ChangeType<in TFrom, out TTo>(TFrom source);
