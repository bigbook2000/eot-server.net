

-- iff > 0 #v_permit_id
	-- set
	UPDATE eox_permit SET
		f_name = '#v_name',
		f_note = '#v_note',
		_update_time = ##now
		WHERE f_permit_id = #v_permit_id;
	-- end
-- end

-- iff <= 0 #v_permit_id
	-- inc #v_permit_id
		INSERT INTO eox_permit(
			f_name,
			f_note,
			_update_time,
			_update_flag) 
            VALUES (
            '#v_name',
			'#v_note',
			##now,
			1);            
	-- end
-- end
    
-- set
SELECT 0 AS _d, '' AS _s, eox_permit.* FROM eox_permit WHERE f_permit_id = #v_permit_id;
-- end