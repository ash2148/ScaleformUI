UIMenuSliderItem = setmetatable({}, UIMenuSliderItem)
UIMenuSliderItem.__index = UIMenuSliderItem
UIMenuSliderItem.__call = function() return "UIMenuItem", "UIMenuSliderItem" end

---New
---@param Text string
---@param Items table
---@param Index number
---@param Description string
---@param Divider boolean
---@param SliderColors table
---@param BackgroundSliderColors table
function UIMenuSliderItem.New(Text, Max, Multiplier, Index, Heritage, Description, color, highlightColor, textColor, highlightedTextColor, sliderColor, backgroundSliderColor)
	local _UIMenuSliderItem = {
		Base = UIMenuItem.New(Text or "", Description or "", color or 117, highlightColor or 1, textColor or 1, highlightedTextColor or 2),
		_Index = tonumber(Index) or 0,
		_Max = tonumber(Max) or 100,
		_Multiplier = Multiplier or 5,
		_heritage = Heritage or false,
		Panels = {},
		SidePanel = nil,
		SliderColor = sliderColor or 116,
		BackgroundSliderColor = backgroundSliderColor or 117,
		OnSliderChanged = function(menu, item, newindex) end,
		OnSliderSelected = function(menu, item, newindex) end,
	}
	return setmetatable(_UIMenuSliderItem, UIMenuSliderItem)
end

---SetParentMenu
---@param Menu table
function UIMenuSliderItem:SetParentMenu(Menu)
	if Menu() == "UIMenu" then
		self.Base.ParentMenu = Menu
	else
		return self.Base.ParentMenu
	end
end

function UIMenuSliderItem:AddSidePanel(sidePanel)
    if sidePanel() == "UIMissionDetailsPanel" then
        sidePanel:SetParentItem(self)
        self.SidePanel = sidePanel
        ScaleformUI.Scaleforms._ui:CallFunction("ADD_SIDE_PANEL_TO_ITEM", false, IndexOf(self.Base.ParentMenu.Items, self) - 1, 0, sidePanel.PanelSide, sidePanel.TitleType, sidePanel.Title, sidePanel.TitleColor, sidePanel.TextureDict, sidePanel.TextureName)
    elseif sidePanel() == "UIVehicleColorPickerPanel" then	
        sidePanel:SetParentItem(self)	
        self.SidePanel = sidePanel	
        ScaleformUI.Scaleforms._ui:CallFunction("ADD_SIDE_PANEL_TO_ITEM", false, IndexOf(self.ParentMenu.Items, self) - 1, 1, sidePanel.PanelSide, sidePanel.TitleType, sidePanel.Title, sidePanel.TitleColor)
	end
end

---Selected
---@param bool table
function UIMenuSliderItem:Selected(bool)
	if bool ~= nil then

		self.Base._Selected = tobool(bool)
	else
		return self.Base._Selected
	end
end

function UIMenuSliderItem:Hovered(bool)
	if bool ~= nil then
		self.Base._Hovered = tobool(bool)
	else
		return self.Base._Hovered
	end
end

function UIMenuSliderItem:Enabled(bool)
	if bool ~= nil then
		self.Base._Enabled = tobool(bool)
	else
		return self.Base._Enabled
	end
end

function UIMenuSliderItem:Description(str)
	if tostring(str) and str ~= nil then
		self.Base._Description = tostring(str)
	else
		return self.Base._Description
	end
end

function UIMenuSliderItem:BlinkDescription(bool)
    if bool ~= nil then
		self.Base:BlinkDescription(bool)
	else
		return self.Base:BlinkDescription()
	end
end

function UIMenuSliderItem:Label(Text)
	if tostring(Text) and Text ~= nil then
		self.Base:Label(tostring(Text))
	else
		return self.Base:Label()
	end
end

function UIMenuItem:MainColor(color)
    if(color)then
        self.Base._mainColor = color
        if(self.Base.ParentMenu ~= nil) then
            ScaleformUI.Scaleforms._ui:CallFunction("UPDATE_COLORS", false, IndexOf(self.Base.ParentMenu.Items, self) - 1, self.Base._mainColor, self.Base._highlightColor, self.Base._textColor, self.Base._highlightedTextColor);
        end
    else
        return self.Base._mainColor
    end
end

function UIMenuItem:TextColor(color)
    if(color)then
        self.Base._textColor = color
        if(self.Base.ParentMenu ~= nil) then
            ScaleformUI.Scaleforms._ui:CallFunction("UPDATE_COLORS", false, IndexOf(self.Base.ParentMenu.Items, self) - 1, self.Base._mainColor, self.Base._highlightColor, self.Base._textColor, self.Base._highlightedTextColor);
        end
    else
        return self.Base._textColor
    end
end

function UIMenuItem:HighlightColor(color)
    if(color)then
        self.Base._highlightColor = color
        if(self.Base.ParentMenu ~= nil) then
            ScaleformUI.Scaleforms._ui:CallFunction("UPDATE_COLORS", false, IndexOf(self.Base.ParentMenu.Items, self) - 1, self.Base._mainColor, self.Base._highlightColor, self.Base._textColor, self.Base._highlightedTextColor);
        end
    else
        return self.Base._highlightColor
    end
end

function UIMenuItem:HighlightedTextColor(color)
    if(color)then
        self.Base._highlightedTextColor = color
        if(self.Base.ParentMenu ~= nil) then
            ScaleformUI.Scaleforms._ui:CallFunction("UPDATE_COLORS", false, IndexOf(self.Base.ParentMenu.Items, self) - 1, self.Base._mainColor, self.Base._highlightColor, self.Base._textColor, self.Base._highlightedTextColor);
        end
    else
        return self.Base._highlightedTextColor
    end
end

function UIMenuSliderItem:Index(Index)
	if tonumber(Index) then
		if tonumber(Index) > self._Max then
			self._Index = self._Max
		elseif tonumber(Index) < 0 then
			self._Index = 0
		else
			self._Index = tonumber(Index)
		end
		self.OnSliderChanged(self.ParentMenu, self, self._Index)
	else
		return self._Index
	end
end

function UIMenuSliderItem:SetLeftBadge()
	error("This item does not support badges")
end

function UIMenuSliderItem:SetRightBadge()
	error("This item does not support badges")
end

function UIMenuSliderItem:RightLabel()
	error("This item does not support a right label")
end