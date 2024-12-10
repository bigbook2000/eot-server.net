
-- iff > 0 #v_role_id
	-- set
	UPDATE eox_role SET
		f_name = '#v_name',
		f_note = '#v_note',
        f_status = #v_status,
		_update_time = ##now
        WHERE f_role_id = #v_role_id;
	-- end
-- end

-- iff <= 0 #v_role_id
	-- inc #v_role_id
	INSERT INTO eox_role(
		f_name,			
		f_note,
        f_permits,
        f_status,            
		_update_time,
		_update_flag) 
        VALUES (
        '#v_name',
		'#v_note',
        '',
        #v_status,
		##now,
		1);
	-- end
-- end
    
-- set
select eox_role.* FROM eox_role WHERE f_role_id = #v_role_id;
-- end