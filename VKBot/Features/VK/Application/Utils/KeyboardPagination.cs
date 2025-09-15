using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;

namespace VKBot.Features.VK.Application.Utils;

public static class KeyboardPagination
{
    private const int ButtonsPerPage = 8;
    private const int ButtonsPerRow = 2;

    public static VkKeyboard CreatePaginatedKeyboard<T>(
        List<T> items, 
        int currentPage, 
        Func<T, string> getButtonText, 
        VkButtonColor buttonColor = VkButtonColor.Primary)
    {
        return CreatePaginatedKeyboard(items, currentPage, getButtonText, out _, buttonColor);
    }
    
    public static VkKeyboard CreatePaginatedKeyboard<T>(
        List<T> items, 
        int currentPage, 
        Func<T, string> getButtonText, 
        out string pageInfo,
        VkButtonColor buttonColor = VkButtonColor.Primary)
    {
        var keyboard = VkKeyboard.Create(false, true);
        
        var totalPages = (int)Math.Ceiling((double)items.Count / ButtonsPerPage);
        var startIndex = currentPage * ButtonsPerPage;
        var endIndex = Math.Min(startIndex + ButtonsPerPage, items.Count);
        
        var pageItems = items.Skip(startIndex).Take(ButtonsPerPage).ToList();
        pageInfo = totalPages > 1 ? $" (стр. {currentPage + 1}/{totalPages})" : "";
        
        for (int i = 0; i < pageItems.Count; i++)
        {
            if (i % ButtonsPerRow == 0) keyboard.AddRow();
            keyboard.AddButton(getButtonText(pageItems[i]), buttonColor);
        }
        
        if (totalPages > 1)
        {
            keyboard.AddRow();
            
            if (currentPage > 0)
                keyboard.AddButton("◀️ Назад", VkButtonColor.Secondary);
            
            if (currentPage < totalPages - 1)
                keyboard.AddButton("Вперёд ▶️", VkButtonColor.Secondary);
        }
        
        return keyboard;
    }
    
    public static bool IsNavigationCommand(string? text, out int direction)
    {
        direction = 0;
        if (text == "◀️ Назад")
        {
            direction = -1;
            return true;
        }
        if (text == "Вперёд ▶️")
        {
            direction = 1;
            return true;
        }
        return false;
    }
    
    public static int GetValidPage(int currentPage, int direction, int totalItems)
    {
        var totalPages = (int)Math.Ceiling((double)totalItems / ButtonsPerPage);
        var newPage = currentPage + direction;
        return Math.Max(0, Math.Min(newPage, totalPages - 1));
    }
}