using System.Text.Json;
using Amazon.SimpleEmailV2.Model;
using DevOpsMcp.Domain.Email;

namespace DevOpsMcp.Infrastructure.Email.Builders;

/// <summary>
/// Builder for AWS SES V2 SendEmailRequest
/// </summary>
internal sealed class SendEmailRequestBuilder
{
    private readonly string _fromAddress;
    private readonly string _fromName;
    private readonly string? _defaultConfigurationSet;
    private readonly List<string> _defaultReplyToAddresses;

    public SendEmailRequestBuilder(
        string fromAddress, 
        string fromName,
        string? defaultConfigurationSet,
        List<string> defaultReplyToAddresses)
    {
        _fromAddress = fromAddress;
        _fromName = fromName;
        _defaultConfigurationSet = defaultConfigurationSet;
        _defaultReplyToAddresses = defaultReplyToAddresses;
    }

    /// <summary>
    /// Build a SendEmailRequest from a domain EmailRequest
    /// </summary>
    public SendEmailRequest Build(EmailRequest request)
    {
        var sendRequest = new SendEmailRequest
        {
            FromEmailAddress = FormatAddress(_fromAddress, _fromName),
            Destination = BuildDestination(request),
            ConfigurationSetName = request.ConfigurationSet ?? _defaultConfigurationSet
        };

        // Set content based on whether using template or raw content
        if (!string.IsNullOrEmpty(request.TemplateName))
        {
            sendRequest.Content = BuildTemplateContent(request);
        }
        else
        {
            sendRequest.Content = BuildSimpleContent(request);
        }

        // Set reply-to addresses
        sendRequest.ReplyToAddresses = GetReplyToAddresses(request.ReplyTo);

        // Add tags if provided
        if (request.Tags.Any())
        {
            sendRequest.EmailTags = request.Tags
                .Select(kvp => new MessageTag { Name = kvp.Key, Value = kvp.Value })
                .ToList();
        }

        return sendRequest;
    }

    private Destination BuildDestination(EmailRequest request)
    {
        return new Destination
        {
            ToAddresses = new List<string> { request.To },
            CcAddresses = request.Cc.ToList(),
            BccAddresses = request.Bcc.ToList()
        };
    }

    private EmailContent BuildTemplateContent(EmailRequest request)
    {
        return new EmailContent
        {
            Template = new Template
            {
                TemplateName = request.TemplateName,
                TemplateData = JsonSerializer.Serialize(request.TemplateData)
            }
        };
    }

    private EmailContent BuildSimpleContent(EmailRequest request)
    {
        var body = new Body
        {
            Html = new Content 
            { 
                Data = request.HtmlContent ?? string.Empty,
                Charset = "UTF-8"
            }
        };

        if (!string.IsNullOrEmpty(request.TextContent))
        {
            body.Text = new Content 
            { 
                Data = request.TextContent,
                Charset = "UTF-8"
            };
        }

        return new EmailContent
        {
            Simple = new Message
            {
                Subject = new Content { Data = request.Subject ?? string.Empty },
                Body = body
            }
        };
    }

    private List<string> GetReplyToAddresses(string? replyTo)
    {
        if (!string.IsNullOrEmpty(replyTo))
            return new List<string> { replyTo };
        
        return _defaultReplyToAddresses.Any() 
            ? _defaultReplyToAddresses 
            : new List<string>();
    }

    private static string FormatAddress(string email, string? name)
    {
        return string.IsNullOrEmpty(name) 
            ? email 
            : $"\"{name}\" <{email}>";
    }
}