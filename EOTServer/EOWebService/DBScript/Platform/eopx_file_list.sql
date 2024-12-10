
---- 列出指定类型和标识的所有文件

-- iff <> '' #v_keyids
    -- set
    select * from eox_file 
        where _update_flag>0 and f_type='#v_type' and f_keyid in (#v_keyids);
    -- end
-- end

---- 返回一个空记录
-- iff = '' #v_keyids
    -- set
    select 0 where 1=0;
    -- end
-- end