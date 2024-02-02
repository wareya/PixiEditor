﻿using System.Runtime.Serialization;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.Extensions.Exceptions;

namespace PixiEditor.AvaloniaUI.Exceptions;

internal class InvalidFileTypeException : RecoverableException
{
    public InvalidFileTypeException() { }

    public InvalidFileTypeException(LocalizedString displayMessage) : base(displayMessage) { }

    public InvalidFileTypeException(LocalizedString displayMessage, Exception innerException) : base(displayMessage, innerException) { }

    public InvalidFileTypeException(LocalizedString displayMessage, string exceptionMessage) : base(displayMessage, exceptionMessage) { }

    public InvalidFileTypeException(LocalizedString displayMessage, string exceptionMessage, Exception innerException) : base(displayMessage, exceptionMessage, innerException) { }

    protected InvalidFileTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

}
