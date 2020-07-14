args = ...

local name_and_create = function(simtype, name, equip)
    local it = PLAYER.cheat_add_item(SIMTYPE_ID(simtype),1)
    ACTOR.set_item_name(it, name)
    if(equip) then
        PLAYER.equip(it, 0)
    end
end

local name_and_create_set = function(base_sim, base_name, equip)
    local parts = {
        ['torso'] = 'Shirt',
        ['head'] ='Cap',
        ['legs'] = 'Bottoms',
        ['hands'] = 'Gloves',
        ['feet'] = 'Boots'
    }
    for k,v in pairs(parts) do 
        name_and_create(base_sim .. k, base_name .. v, equip)
    end
end

name_and_create_set('glowing_warrior_', 'Glowing Warrior ', false)
name_and_create_set('glowing_rogue_', 'Glowing Rogue ', false)

name_and_create_set('Clothing_peasant03_', 'Generic Peasant ', false)
name_and_create_set('Clothing_peasant04_', 'Dokkalfar Noble ', false)
name_and_create_set('Clothing_peasant05_', 'Dokkalfar Alt Noble ', false)
name_and_create_set('Clothing_peasant06_', 'Dokkalfar Peasant ', true)
name_and_create_set('Clothing_peasant07_', 'Dokkalfar Alt Peasant ', false)
name_and_create_set('Clothing_peasant08_', 'Dokkalfar Merchant ', false)
name_and_create_set('Clothing_peasant09_', 'Dokkalfar Alt Merchant ', false)

name_and_create_set('Clothing_peasant10_', 'Ljosalfar Noble ', false)
name_and_create_set('Clothing_peasant11_', 'Ljosalfar Alt Noble ', false)
name_and_create_set('Clothing_peasant12_', 'Ljosalfar Peasant ', false)
name_and_create_set('Clothing_peasant13_', 'Ljosalfar Alt Peasant ', false)
name_and_create_set('Clothing_peasant14_', 'Ljosalfar Merchant ', false)
name_and_create_set('Clothing_peasant15_', 'Ljosalfar Alt Merchant ', false)

name_and_create_set('Clothing_peasant16_', 'Almain Merchant ', false)
name_and_create_set('Clothing_peasant17_', 'Almain Alt Merchant ', false)
name_and_create_set('Clothing_peasant18_', 'Verani Merchant ', false)
name_and_create_set('Clothing_peasant19_', 'Verani Alt Merchant ', false)

name_and_create_set('Clothing_peasant20_', 'Almain Noble ', false)
name_and_create_set('Clothing_peasant21_', 'Almain Alt Noble ', false)
name_and_create_set('Clothing_peasant22_', 'Verani Noble ', false)
name_and_create_set('Clothing_peasant23_', 'Verani Alt Noble ', false)
